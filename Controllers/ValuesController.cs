using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Xml;
using LD.IdentityManagement.API.Models;
using System.Security.Claims;
using System.Threading.Tasks;


namespace LD.IdentityManagement.API.Controllers
{

    public static class Validateclass
    {
        public static Regex Regex_select = new Regex(@"('(''|[^'])*')|(;)|(--)|(%)|(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE){0,1}|INSERT( +INTO){0,1}|MERGE|SELECT|UPDATE|UNION( +ALL){0,1})\b)", RegexOptions.Compiled);

        public static HttpResponseMessage ValidateInput(string input, HttpRequestMessage Request)
        {
            HttpResponseMessage errorresponse = null;
            if (Regex_select.Match(input).Success)
            {
                errorresponse = Request.CreateResponse(HttpStatusCode.BadRequest);
                errorresponse.Content = new StringContent("Forbidden request query");
            }
            return errorresponse;
        }
    }

    /*public class RequireIPAttribute : System.Web.Http.Filters.AuthorizationFilterAttribute
    {
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext HttpActionContext)
        {
            LD.IdentityManagement.API.Startup.accesslog.Info("MS_OwinContext");
            //ClaimsIdentity identity = (ClaimsIdentity)HttpContext.Current.User.Identity
            if (HttpActionContext.Request.Properties.Keys.Contains("MS_OwinContext"))
            {
                Microsoft.Owin.IOwinContext temp = HttpActionContext.Request.GetOwinContext();//?
                ClaimsPrincipal user = temp.Authentication.User;

                Microsoft.Owin.IOwinContext caller = ((Microsoft.Owin.IOwinContext)HttpActionContext.Request.Properties["MS_OwinContext"]);
                ClaimsIdentity identity = (ClaimsIdentity)caller.Request.User.Identity;

                //string Ipstr = caller.Request.UserHostAddress;
                //Claim UserNameClaim = ClaimsIdentity.FindFirst(c => c.Type == "UserName");
                if (identity.HasClaim(c => c.Type == "IpAddress" && c.Value != (caller.Request.LocalIpAddress)))
                {
                    LD.IdentityManagement.API.Startup.accesslog.Info("invalid claim IP from user {0} from {1}", caller.Request.User.Identity.Name, (caller.Request.LocalIpAddress));

                    HttpActionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
                    {
                        ReasonPhrase = "Claim from wrong host"
                    };
                }
                LD.IdentityManagement.API.Startup.accesslog.Info("{0} query {1}", identity.Name, HttpActionContext.Request.RequestUri.ToString());
            }
            else
            {
                LD.IdentityManagement.API.Startup.accesslog.Info("{0} query {1}", "", HttpActionContext.Request.RequestUri.ToString());
            }
            base.OnAuthorization(HttpActionContext);

        }
    }*/

    /*public class accesslog
    {
        public static void Logquery(System.Security.Principal.IIdentity Identity, string query)
        {
           //if (Identity.AuthenticationType == "Bearer")
           //{
           //    ClaimsIdentity ClaimsIdentity = (ClaimsIdentity)Identity;
           //    Claim UserNameClaim = ClaimsIdentity.FindFirst(c => c.Type == "UserName");
           //    LD.IdentityManagement.API.Startup.accesslog.Info("{0} query {1}", UserNameClaim.Value, query);
           //}
           //else
           //    LD.IdentityManagement.API.Startup.accesslog.Info("{0} query {1}", Identity.Name, query);
            //LD.IdentityManagement.API.Startup.accesslog.Info("{0} query {1}", Identity.Name, query);
        }
    }*/

    /*[Authorize]
    public class TestController : ApiController
    {
        [Route("api/test")]
        [HttpGet]
        [RequireIP]
        public IHttpActionResult test()
        {
            ClaimsIdentity identity = (ClaimsIdentity)User.Identity;

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("Ok");
            return Ok(identity.Claims);
        }

        [Route("api/testrole")]
        [HttpGet]
        [Authorize(Roles = "user")]
        public IHttpActionResult testrole()
        {
            ClaimsIdentity identity = (ClaimsIdentity)User.Identity;

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("Ok");
            return Ok(identity.Claims);
        }
    }*/

    [Authorize]
    public class MetaverseController : ApiController
    {
        [Route("api/metaverse")]
        [HttpGet]
        public async Task<HttpResponseMessage> metaverse()
        {
            Task<HttpResponseMessage> task = Task<HttpResponseMessage>.Factory.StartNew(() =>
            {
                try
                {
                    stats RunStat = new stats();
                    RunStat.starttime = DateTime.Now;



                    HttpResponseMessage errorresponse = Validateclass.ValidateInput(this.Request.RequestUri.ToString(), Request);
                    if (errorresponse != null)
                        return errorresponse;


                    //Get String Query Pairs
                    Dictionary<string, string> queryString = this.Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
                    string temp;

                    //Get trace value
                    bool trace = false;
                    if (queryString.TryGetValue("trace", out temp))
                    {
                        try
                        {
                            trace = bool.Parse(temp);
                            RunStat.info = new Dictionary<string, string>();
                        }
                        catch { }
                    }

                    //Get private attributs
                    //check access to private attributs
                    bool privateattributs = false;
                    if (queryString.TryGetValue("private", out temp))
                    {
                        //Cheack user access 
                        try
                        {
                            privateattributs = bool.Parse(temp) && (User.IsInRole("GLTFIMAdmins") || User.Identity.Name == "aseand");
                        }
                        catch { }
                    }

                    //Get query attribus and valus
                    bool attribute = false;
                    bool GetConnectedMA = false;
                    List<string> SingleAttributs = new List<string>();
                    List<string> MulitAttributes = new List<string>();
                    string SingelValueQuery = "";
                    string MulitValueQuery = "";
                    List<Guid> MulitValueobject_id = new List<Guid>();
                    foreach (string key in queryString.Keys)
                    {
                        LD.IdentityManagement.API.Models.Attribute Attribute;
                        if (LD.IdentityManagement.API.Startup.Schema_Attributes.TryGetValue(key.ToLower(), out Attribute))
                        {
                            // build databas query from query string
                            if (Attribute.mulitvalue)
                            {
                                if (MulitValueQuery.Length > 0)
                                    MulitValueQuery += " AND";
                                string[] split = queryString[Attribute.name].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                MulitValueQuery += string.Format(" attribute_name = '{0}' AND string_value_indexable in ('{1}')", Attribute.name, string.Join("','", split));
                            }
                            else
                            {
                                if (SingelValueQuery.Length > 0)
                                    SingelValueQuery += " AND";
                                string[] split = queryString[Attribute.name].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                SingelValueQuery += string.Format(" [{0}] in ('{1}')", Attribute.name, string.Join("','", split));
                            }
                        }
                        else //Special query
                        {
                            switch (key.ToLower())
                            {
                                case "attribute":
                                    attribute = true;
                                    string[] splitattribute = queryString[key].ToLower().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach (string Value in splitattribute)
                                    {
                                        if (LD.IdentityManagement.API.Startup.Schema_Attributes.TryGetValue(Value, out Attribute))
                                        {
                                            if (Attribute.mulitvalue)
                                                MulitAttributes.Add(Value);
                                            else
                                                SingleAttributs.Add(Value);
                                        }//private access?
                                        else if (privateattributs)
                                        {
                                            if (LD.IdentityManagement.API.Startup.Schema_Private_Attributes.TryGetValue(Value, out Attribute))
                                            {
                                                if (Attribute.mulitvalue)
                                                    MulitAttributes.Add(Value);
                                                else
                                                    SingleAttributs.Add(Value);
                                            }
                                        }
                                    }
                                    break;

                                case "connectedma":
                                    try { GetConnectedMA = bool.Parse(queryString[key]); }
                                    catch { }
                                    break;

                                case "object_id":
                                    if (SingelValueQuery.Length > 0)
                                        SingelValueQuery += " OR";
                                    string[] splitID = queryString[key].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    SingelValueQuery += " object_id = '" + string.Join("' OR object_id = '", splitID) + "'";
                                    break;

                                case "object_type":
                                    if (SingelValueQuery.Length > 0)
                                        SingelValueQuery += " AND";
                                    string[] split = queryString[key].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    SingelValueQuery += string.Format(" object_type in ('{0}')", string.Join("','", split));
                                    break;

                                case "last_modification_date":
                                    if (SingelValueQuery.Length > 0)
                                        SingelValueQuery += " AND";
                                    SingelValueQuery += string.Format(" last_modification_date > '{0}'", queryString[key]);
                                    break;
                            }
                        }
                    }

                    //No Attributs? Select all
                    if (!attribute)
                    {
                        foreach (LD.IdentityManagement.API.Models.Attribute Attribute in LD.IdentityManagement.API.Startup.Schema_Attributes.Values)
                        {

                            bool privateatt = LD.IdentityManagement.API.Startup.Schema_Private_Attributes.ContainsKey(Attribute.name);
                            if (!privateatt || privateattributs && privateatt)
                            {
                                if (Attribute.mulitvalue)
                                    MulitAttributes.Add(Attribute.name);
                                else
                                    SingleAttributs.Add(Attribute.name);
                            }
                        }
                    }
                    if (trace)
                    {
                        RunStat.info.Add("singleAattributs", string.Join(",", SingleAttributs));
                        RunStat.info.Add("mulitattributes", string.Join(",", MulitAttributes));
                        RunStat.info.Add("getconnectedma", GetConnectedMA.ToString());
                        RunStat.info.Add("privateattributs", privateattributs.ToString());
                        RunStat.info.Add("requesturi", this.Request.RequestUri.ToString());
                        RunStat.info.Add("singelvaluequery", SingelValueQuery);
                        RunStat.info.Add("mulitvaluequery", MulitValueQuery);
                    }

                    //exec query
                    System.Data.DataRow[] mms_metaverse_rows = null;

                    using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.AppSettings["FIMSynchronizationService"]))
                    {
                        connection.Open();
                        using (System.Data.SqlClient.SqlCommand SqlCommand = new System.Data.SqlClient.SqlCommand(
                            string.Format("SELECT object_id,object_type,last_modification_date,[{0}] FROM mms_metaverse (nolock)", string.Join("],[", SingleAttributs)),
                            connection))
                        {
                            if (SingelValueQuery.Length > 0)
                            {
                                SqlCommand.CommandText += string.Format(" WHERE {0}", SingelValueQuery);
                            }

                            if (MulitValueQuery.Length > 0)
                            {
                                SqlCommand.CommandText += string.Format(" AND object_id in (SELECT object_id FROM mms_metaverse_multivalue (nolock) WHERE {0})", MulitValueQuery);
                            }
                            //LD.IdentityManagement.API.Startup.logger.Info(SqlCommand.CommandText);
                            using (SqlDataAdapter SqlDataAdapter = new SqlDataAdapter(SqlCommand))
                            {
                                using (System.Data.DataTable mms_metaverse = new System.Data.DataTable("mms_metaverse"))
                                {
                                    if (SqlDataAdapter.Fill(mms_metaverse) > 0)
                                    {
                                        //mms_metaverse_rows = mms_metaverse.Rows.AsQueryable().OfType<System.Data.DataRow>().ToArray();
                                        mms_metaverse_rows = new System.Data.DataRow[mms_metaverse.Rows.Count];
                                        mms_metaverse.Rows.CopyTo(mms_metaverse_rows, 0);
                                    }
                                    else
                                        mms_metaverse_rows = new System.Data.DataRow[0];
                                }
                            }
                        }



                        //Build out object class
                        List<metaverse> returnObject = new List<metaverse>();
                        for (int i = 0; i < mms_metaverse_rows.Length; i++)
                        {
                            Guid ObjectGuid = (Guid)mms_metaverse_rows[i]["object_id"];

                            //Skip if mulit quary is activ and Guid is not in list
                            if (MulitValueobject_id.Count > 0 && !MulitValueobject_id.Contains(ObjectGuid))
                                continue;

                            //New object
                            metaverse Object = new metaverse()
                            {
                                object_id = ObjectGuid,
                                object_type = (string)mms_metaverse_rows[i]["object_type"],
                                last_modification_date = (DateTime)mms_metaverse_rows[i]["last_modification_date"],
                                attributes = new Dictionary<string, string[]>()
                            };

                            //single 
                            foreach (string name in SingleAttributs)
                            {
                                if (mms_metaverse_rows[i][name] != System.DBNull.Value)
                                    Object.attributes.Add(name, new string[] { mms_metaverse_rows[i][name].ToString() });
                            }

                            //mulit
                            if (MulitAttributes.Count > 0)
                            {

                                using (System.Data.SqlClient.SqlCommand SqlCommand = new System.Data.SqlClient.SqlCommand(
                                    string.Format("SELECT attribute_name,string_value_indexable FROM mms_metaverse_multivalue (nolock) WHERE object_id = '{0}'", ObjectGuid.ToString()),
                                    connection))
                                {
                                    //LD.IdentityManagement.API.Startup.logger.Info(SqlCommand.CommandText);
                                    using (SqlDataAdapter SqlDataAdapter = new SqlDataAdapter(SqlCommand))
                                    {
                                        using (System.Data.DataTable mms_metaverse_multivalue_temp = new System.Data.DataTable("mms_metaverse_multivalue"))
                                        {
                                            if (SqlDataAdapter.Fill(mms_metaverse_multivalue_temp) > 0)
                                            {
                                                Dictionary<string, List<string>> list = new Dictionary<string, List<string>>();
                                                foreach (string name in MulitAttributes)
                                                    list.Add(name, new List<string>());

                                                List<string> templist;
                                                foreach (System.Data.DataRow row in mms_metaverse_multivalue_temp.Rows)
                                                {
                                                    if (list.TryGetValue(((string)row["attribute_name"]).ToLower(), out templist))
                                                        templist.Add(row["string_value_indexable"].ToString());
                                                }

                                                //Add values
                                                foreach (string key in list.Keys)
                                                    if (list[key].Count > 0)
                                                        Object.attributes.Add(key, list[key].ToArray());
                                            }
                                        }
                                    }
                                }
                            }


                            //Get ConnectedMA relations
                            if (GetConnectedMA)
                            {
                                using (System.Data.SqlClient.SqlCommand SqlCommand = new System.Data.SqlClient.SqlCommand(
                                    string.Format("SELECT cs.ma_id,cs.object_id FROM [FIMSynchronizationService].[dbo].mms_csmv_link csmv (nolock) join [FIMSynchronizationService].[dbo].mms_connectorspace cs (nolock) on csmv.cs_object_id = cs.object_id where csmv.mv_object_id = '{0}' order by 1", Object.object_id.ToString()),
                                    connection))
                                {
                                    using (SqlDataAdapter SqlDataAdapter = new SqlDataAdapter(SqlCommand))
                                    {
                                        using (System.Data.DataTable mms_csmv_link = new System.Data.DataTable("mms_csmv_link"))
                                        {
                                            if (SqlDataAdapter.Fill(mms_csmv_link) > 0)
                                            {
                                                Dictionary<Guid, List<Guid>> tempMAlist = new Dictionary<Guid, List<Guid>>();
                                                foreach (System.Data.DataRow row in mms_csmv_link.Rows)
                                                {
                                                    Guid maid = (Guid)row["ma_id"];
                                                    List<Guid> templist;
                                                    if (tempMAlist.TryGetValue(maid, out templist))
                                                    {
                                                        templist.Add((Guid)row["object_id"]);
                                                    }
                                                    else
                                                    {
                                                        tempMAlist.Add(maid, new List<Guid>());
                                                        tempMAlist[maid].Add((Guid)row["object_id"]);
                                                    }
                                                }

                                                Object.connectedma = new List<connectedma>();
                                                foreach (Guid maid in tempMAlist.Keys)
                                                {
                                                    Object.connectedma.Add(new connectedma()
                                                    {
                                                        ma_id = maid,
                                                        ma_name = LD.IdentityManagement.API.Startup.Schema_managementagent[maid].name,
                                                        connectorobject = tempMAlist[maid]
                                                    }
                                                    );
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            returnObject.Add(Object);
                        }
                        //Serialize MV
                        string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(returnObject.ToArray());

                        //Set exec time
                        RunStat.endtime = DateTime.Now;
                        RunStat.exectime = RunStat.endtime - RunStat.starttime;
                        RunStat.resualtcount = returnObject.Count;
                        returnObject = null;
                        LD.IdentityManagement.API.Startup.logger.Info("metaverse exectime: {0}", RunStat.exectime);

                        HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                        response.Content = new StringContent(string.Format("{{\"stats\":{0},\"metaverse\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                        return response;
                    }
                }
                catch (Exception e)
                {
                    LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                    LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                    LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(e.Message);
                    return response;
                }
            });
            await task;
            return task.Result;

        }

        [Route("api/metaverse/search")]
        [HttpGet]
        public async Task<HttpResponseMessage> search()
        {
            Task<HttpResponseMessage> task = Task<HttpResponseMessage>.Factory.StartNew(() =>
            {
                try
                {
                    stats RunStat = new stats();
                    RunStat.starttime = DateTime.Now;
                    RunStat.info = new Dictionary<string, string>();

                    

                    HttpResponseMessage errorresponse = Validateclass.ValidateInput(this.Request.RequestUri.ToString(), Request);
                    if (errorresponse != null)
                        return errorresponse;

                    Dictionary<string, string> queryString = this.Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
                    int maxcount = 30;
                    string seachvalue = "";
                    bool trace = false;
                    HashSet<string[]> seachpair = new HashSet<string[]>();
                    foreach (string key in queryString.Keys)
                    {
                        switch (key)
                        {
                            case "maxcount":
                                maxcount = int.Parse(queryString[key]);
                                break;
                            case "seachpair":
                                foreach (string pair in queryString[key].ToLower().Split(' '))
                                    seachpair.Add(pair.Split(':'));
                                break;
                            case "seachvalue":
                                seachvalue = queryString[key];
                                break;
                            case "trace":
                                trace = bool.Parse(queryString[key]);
                                break;
                            //default:
                            //break;
                        }

                    }

                    if (trace)
                    {
                        RunStat.info.Add("seachpair", seachpair.Count.ToString());
                    }

                    List<System.Data.DataRow> rowlist = new List<System.Data.DataRow>();
                    Dictionary<Guid, string> returnList = new Dictionary<Guid, string>();
                    string selectQuery = "";
                    int count = 0;
                    int pairindex = 0;
                    foreach (string[] pair in seachpair)
                    {
                        if (trace)
                        {
                            RunStat.info.Add(pairindex.ToString(), string.Join(",", pair));
                        }
                        pairindex++;

                        List<string> tempList = new List<string>();
                        tempList.Add(seachvalue);
                        tempList.Add(maxcount.ToString());
                        tempList.AddRange(pair);

                        LD.IdentityManagement.API.Models.Attribute attr;
                        LD.IdentityManagement.API.Models.classobject classobject;

                        //Check if dont exist in Schema_Private_Attributes and exist in Schema_Attributes
                        //Last attr from Schema_Attributes is use i query
                        if (pair.Length > 1 && (LD.IdentityManagement.API.Startup.Schema_Private_Attributes.TryGetValue(pair[1], out attr) || !LD.IdentityManagement.API.Startup.Schema_Attributes.TryGetValue(pair[1], out attr)))
                            continue;
                        //Schema_Object exist check
                        if (pair.Length > 2 && !LD.IdentityManagement.API.Startup.Schema_Object.TryGetValue(pair[2], out classobject))
                            continue;
                        //seach attribute check, use i query
                        if (LD.IdentityManagement.API.Startup.Schema_Private_Attributes.TryGetValue(pair[0], out attr) || !LD.IdentityManagement.API.Startup.Schema_Attributes.TryGetValue(pair[0], out attr))
                            continue;

                        selectQuery += selectQuery.Length > 0 ? " UNION " : "";

                        if (attr.mulitvalue)
                        {
                            switch (pair.Length)
                            {
                                case 1:
                                    selectQuery += string.Format("SELECT top {1} object_id ,string_value_indexable FROM mms_metaverse_multivalue (nolock) WHERE attribute_name = '{2}' and string_value_indexable like '%{0}%'", tempList.ToArray());
                                    break;
                            }
                        }
                        else
                        {
                            switch (pair.Length)
                            {
                                case 1:
                                    selectQuery += string.Format("SELECT top {1} object_id,{2} FROM mms_metaverse (nolock) WHERE {2} is not null and {2} like '%{0}%'", tempList.ToArray());
                                    break;
                                case 2:
                                    selectQuery += string.Format("SELECT top {1} object_id,{2}+' | '+{3} FROM mms_metaverse (nolock) WHERE {3} is not null and {2} like '%{0}%'", tempList.ToArray());
                                    break;
                                case 3:
                                    selectQuery += string.Format("SELECT top {1} object_id,{2}+' | '+{3} FROM mms_metaverse (nolock) WHERE {3} is not null and {2} like '%{0}%' and object_type = '{4}'", tempList.ToArray());
                                    //
                                    break;
                            }

                        }
                        //}
                        if (count >= maxcount)
                            break;
                    }

                    if (trace)
                        RunStat.info.Add("selectQuery", selectQuery);

                    using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.AppSettings["FIMSynchronizationService"]))
                    {
                        connection.Open();
                        using (System.Data.SqlClient.SqlCommand SqlCommand = new System.Data.SqlClient.SqlCommand(selectQuery, connection))
                        {
                            using (System.Data.SqlClient.SqlDataReader SqlDataReader = SqlCommand.ExecuteReader())
                            {
                                while (SqlDataReader.Read())
                                {
                                    try
                                    {
                                        returnList.Add((Guid)SqlDataReader[0], SqlDataReader[1] == System.DBNull.Value ? "" : SqlDataReader[1].ToString());
                                    }
                                    catch //(Exception e)
                                    {
                                        // RunStat.info.Add("Exception", e.Message);
                                    }

                                }
                            }
                        }
                    }



                    string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(returnList);

                    //Set exec time
                    RunStat.endtime = DateTime.Now;
                    RunStat.exectime = RunStat.endtime - RunStat.starttime;
                    RunStat.resualtcount = returnList.Count;
                    LD.IdentityManagement.API.Startup.logger.Info("metaverse/search exectime: {0}", RunStat.exectime);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(string.Format("{{\"stats\":{0},\"search\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                    return response;
                    //return returnList;

                }
                catch (Exception e)
                {
                    LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                    LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                    LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.ExpectationFailed);
                    response.Content = new StringContent(e.Message);
                    return response;
                }
            });
            await task;
            return task.Result;
        }
    }

    [Authorize(Roles = @"DOMAIN\FIMADMINS,DOMAIN\FIMSYS")]
    public class connectorspaceController : ApiController
    {
        [Route("api/connectorspace/{id}")]
        [HttpGet]
        public async Task<HttpResponseMessage> connectorspace(string id)
        {
            Task<HttpResponseMessage> task = Task<HttpResponseMessage>.Factory.StartNew(() =>
            {
                try
                {
                    stats RunStat = new stats();
                    RunStat.starttime = DateTime.Now;

                    

                    HttpResponseMessage errorresponse = Validateclass.ValidateInput(this.Request.RequestUri.ToString(), Request);
                    if (errorresponse != null)
                        return errorresponse;


                    string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(connectorspaceController.getCS(id));

                    //Set exec time
                    RunStat.endtime = DateTime.Now;
                    RunStat.exectime = RunStat.endtime - RunStat.starttime;
                    RunStat.resualtcount = 1;
                    LD.IdentityManagement.API.Startup.logger.Info("connectorspace exectime: {0}", RunStat.exectime);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(string.Format("{{\"stats\":{0},\"connectorspace\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                    return response;
                    //return Validateclass.getCS(id);
                }
                catch (Exception e)
                {
                    LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                    LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                    LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(e.Message);
                    return response;
                }
            });
            await task;
            return task.Result;
        }

        [Route("api/connectorspace/history/{id}")]
        [HttpGet]
        public async Task<HttpResponseMessage> connectorspacehistory(string id)
        {
            Task<HttpResponseMessage> task = Task<HttpResponseMessage>.Factory.StartNew(() =>
            {
                try
                {
                    stats RunStat = new stats();
                    RunStat.starttime = DateTime.Now;

                    

                    HttpResponseMessage errorresponse = Validateclass.ValidateInput(this.Request.RequestUri.ToString(), Request);
                    if (errorresponse != null)
                        return errorresponse;

                    connectorspacehistory returnObject = new connectorspacehistory();

                    //get connectorspace and hologram
                    returnObject.connectorspace = connectorspaceController.getCS(id);

                    List<connectorspace> list = new List<connectorspace>();
                    //Get all history from mms_connectorspace_history 
                    using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.AppSettings["miisExtraFunctions"]))
                    {
                        connection.Open();
                        string deltaColumn = System.Configuration.ConfigurationManager.AppSettings["deltaColumn"];
                        using (System.Data.SqlClient.SqlCommand SqlCommand = new System.Data.SqlClient.SqlCommand(
                            string.Format("select object_id,ma_id,rdn,export_operation,last_import_modification_date,last_export_modification_date,{3} from mms_connectorspace_history (nolock) where (object_id = '{0}' or rdn = '{1}') and ma_id = '{2}' order by [last_import_modification_date] desc,[last_export_modification_date] desc", id, returnObject.connectorspace.rdn, returnObject.connectorspace.ma_id, deltaColumn),
                            connection))
                        {
                            using (System.Data.SqlClient.SqlDataReader SqlDataReader = SqlCommand.ExecuteReader())
                            {
                                while (SqlDataReader.Read())
                                {
                                    connectorspace connectorspace = new connectorspace();
                                    connectorspace.object_id = (Guid)SqlDataReader["object_id"];
                                    connectorspace.ma_id = (Guid)SqlDataReader["ma_id"];
                                    connectorspace.ma_name = LD.IdentityManagement.API.Startup.Schema_managementagent[connectorspace.ma_id].name;
                                    connectorspace.rdn = (string)SqlDataReader["rdn"];
                                    if (SqlDataReader["last_import_modification_date"] != System.DBNull.Value)
                                    {
                                        connectorspace.last_import_modification_date = (DateTime)SqlDataReader["last_import_modification_date"];
                                    }
                                    if (SqlDataReader["last_export_modification_date"] != System.DBNull.Value)
                                    {
                                        connectorspace.last_export_modification_date = (DateTime)SqlDataReader["last_export_modification_date"];
                                    }

                                    //Decompress delta XML
                                    if (SqlDataReader[deltaColumn] != System.DBNull.Value && ((byte[])SqlDataReader[deltaColumn]).Length > 0)
                                    {
                                        using (System.IO.MemoryStream MemoryStream = new System.IO.MemoryStream((byte[])SqlDataReader[deltaColumn]))
                                        {
                                            using (System.IO.Compression.GZipStream GZipStream = new System.IO.Compression.GZipStream(MemoryStream, System.IO.Compression.CompressionMode.Decompress))
                                            {
                                                using (System.IO.StreamReader StreamReader = new System.IO.StreamReader(GZipStream, System.Text.Encoding.Default))
                                                {
                                                    connectorspace.data = new XmlDocument();
                                                    connectorspace.data.Load(StreamReader);
                                                }
                                            }
                                        }
                                    }
                                    list.Add(connectorspace);
                                }
                            }
                        }
                        connection.Close();
                    }
                    if (list.Count > 0)
                        returnObject.history = list.ToArray();
                    else
                        returnObject.history = null;

                    string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(returnObject);

                    //Set exec time
                    RunStat.endtime = DateTime.Now;
                    RunStat.exectime = RunStat.endtime - RunStat.starttime;
                    RunStat.resualtcount = list.Count + 1;
                    LD.IdentityManagement.API.Startup.logger.Info("connectorspace/history exectime: {0}", RunStat.exectime);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(string.Format("{{\"stats\":{0},\"connectorspacehistory\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                    return response;
                    //return returnObject;

                }
                catch (Exception e)
                {
                    LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                    LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                    LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                    HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new StringContent(e.Message);
                    return response;
                }
            });
            await task;
            return task.Result;
        }

        public static connectorspace getCS(string guid)
        {
            connectorspace connectorspace = null;

            using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.AppSettings["FIMSynchronizationService"]))
            {
                connection.Open();
                using (System.Data.SqlClient.SqlCommand SqlCommand = new System.Data.SqlClient.SqlCommand(
                    string.Format("select object_id,ma_id,rdn,last_import_modification_date,last_export_modification_date from mms_connectorspace (nolock) where object_id = '{0}'", guid),
                    connection))
                {
                    using (System.Data.SqlClient.SqlDataReader SqlDataReader = SqlCommand.ExecuteReader())
                    {
                        if (SqlDataReader.Read())
                        {
                            connectorspace = new connectorspace();
                            connectorspace.object_id = (Guid)SqlDataReader["object_id"];
                            connectorspace.ma_id = (Guid)SqlDataReader["ma_id"];
                            connectorspace.ma_name = LD.IdentityManagement.API.Startup.Schema_managementagent[connectorspace.ma_id].name;
                            connectorspace.rdn = (string)SqlDataReader["rdn"];
                            connectorspace.last_import_modification_date = (DateTime)SqlDataReader["last_import_modification_date"];
                            if (SqlDataReader["last_export_modification_date"] != System.DBNull.Value)
                            {
                                connectorspace.last_export_modification_date = (DateTime)SqlDataReader["last_export_modification_date"];
                            }

                            //Get hologram from MIM
                            Microsoft.DirectoryServices.MetadirectoryServices.UI.WebServices.MMSWebService MMSWebService = new Microsoft.DirectoryServices.MetadirectoryServices.UI.WebServices.MMSWebService();
                            connectorspace.data = new XmlDocument();
                            connectorspace.data.LoadXml(MMSWebService.GetCSObjects(new string[] { guid }, 1, Microsoft.DirectoryServices.MetadirectoryServices.UI.PropertySheetBase.CSElementBitMask.CS_ELEMENT_HOLOGRAM, 17, 0, null));

                        }
                    }
                }
                connection.Close();
            }

            return connectorspace;
        }
    }

    [Authorize]
    public class SchemaController : ApiController
    {
        [Route("api/schema/attributes")]
        [HttpGet]
        public HttpResponseMessage attributes()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                

                HttpResponseMessage errorresponse = Validateclass.ValidateInput(this.Request.RequestUri.ToString(), Request);
                if (errorresponse != null)
                    return errorresponse;

                LD.IdentityManagement.API.Models.Attribute[] returnList;

                Dictionary<string, string> queryString = this.Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
                string temp;

                //query by attribute
                if (queryString.TryGetValue("attribute", out temp))
                {
                    string[] split = temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    returnList = LD.IdentityManagement.API.Startup.Schema_Attributes.Where(k => split.Contains(k.Key)).Select(x => x.Value).ToArray();
                }
                else
                    returnList = LD.IdentityManagement.API.Startup.Schema_Attributes.Values.ToArray();

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(returnList);

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("schema/attributes exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"attributes\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }

        [Route("api/schema/managementagent")]
        [HttpGet]
        public HttpResponseMessage managementagent()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                

                HttpResponseMessage errorresponse = Validateclass.ValidateInput(this.Request.RequestUri.ToString(), Request);
                if (errorresponse != null)
                    return errorresponse;

                //test
                if (User.Identity.Name != "aseand")
                {
                    //return null;
                }

                if (!User.IsInRole("some role"))
                {
                    //return null;
                }


                managementagent[] returnList;

                Dictionary<string, string> queryString = this.Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
                string temp;

                //by guid or name
                if (queryString.TryGetValue("guid", out temp))
                {
                    string[] split = temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    returnList = LD.IdentityManagement.API.Startup.Schema_managementagent.Where(k => split.Contains(k.Key.ToString())).Select(x => x.Value).ToArray();
                }
                else if (queryString.TryGetValue("name", out temp))
                {
                    string[] split = temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    returnList = LD.IdentityManagement.API.Startup.Schema_managementagent.Where(k => split.Contains(k.Value.name)).Select(x => x.Value).ToArray();
                }
                else
                    returnList = LD.IdentityManagement.API.Startup.Schema_managementagent.Values.ToArray();

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(returnList);

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("schema/managementagent exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"managementagent\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;            
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }

        [Route("api/schema/class")]
        [HttpGet]
        public HttpResponseMessage classGet()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                HttpResponseMessage errorresponse = Validateclass.ValidateInput(this.Request.RequestUri.ToString(), Request);
                if (errorresponse != null)
                    return errorresponse;

                classobject[] returnList;

                //Get query
                Dictionary<string, string> queryString = this.Request.GetQueryNameValuePairs().ToDictionary(x => x.Key, x => x.Value);
                string temp;

                //query class
                if (queryString.TryGetValue("class", out temp))
                {
                    string[] split = temp.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    returnList = LD.IdentityManagement.API.Startup.Schema_Object.Where(k => split.Contains(k.Key)).Select(x => x.Value).ToArray();
                }
                else
                    returnList = LD.IdentityManagement.API.Startup.Schema_Object.Values.ToArray();

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject(returnList);

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("schema/class exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"class\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }
    }

    [Authorize]
    public class OperationsController : ApiController
    {
        [Route("api/operations/report")]
        [HttpGet]
        public HttpResponseMessage report()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                

                //Get MIM status
                //Cpu/memmory/disk?

                //Agents status
                //List agents + last error
                //Running agents

                //Total runns (history runns?)
                //Total objects
                //Object by type
                //

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject("");

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = 0;//returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("operations/report exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"attributes\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }

        [Route("api/operations/log")]
        [HttpGet]
        public HttpResponseMessage log()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject("");

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = 0;//returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("operations/log exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"attributes\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }

        [Route("api/operations/error")]
        [HttpGet]
        public HttpResponseMessage error()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject("");

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = 0;//returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("operations/error exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"attributes\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }

        [Authorize(Roles = @"DOMAIN\FIMADMINS,DOMAIN\FIMSYS")]
        [Route("api/operations/run")]
        [HttpGet]
        public HttpResponseMessage run()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject("");

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = 0;//returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("operations/run exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"attributes\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }

        [Authorize(Roles = @"DOMAIN\FIMADMINS,DOMAIN\FIMSYS")]
        [Route("api/operations/getrun")]
        [HttpGet]
        public HttpResponseMessage getrun()
        {
            try
            {
                stats RunStat = new stats();
                RunStat.starttime = DateTime.Now;

                string SerializeObjectString = Newtonsoft.Json.JsonConvert.SerializeObject("");

                //Set exec time
                RunStat.endtime = DateTime.Now;
                RunStat.exectime = RunStat.endtime - RunStat.starttime;
                RunStat.resualtcount = 0;//returnList.Length;
                LD.IdentityManagement.API.Startup.logger.Info("operations/getrun exectime: {0}", RunStat.exectime);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(string.Format("{{\"stats\":{0},\"attributes\":{1}}}", (Newtonsoft.Json.JsonConvert.SerializeObject(RunStat)), SerializeObjectString), System.Text.Encoding.UTF8, "application/json");
                return response;
                //return returnList;
            }
            catch (Exception e)
            {
                LD.IdentityManagement.API.Startup.logger.Info(e.Message);
                LD.IdentityManagement.API.Startup.logger.Info(e.Source);
                LD.IdentityManagement.API.Startup.logger.Info(e.StackTrace);

                HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(e.Message);
                return response;
            }
        }
    }
}
