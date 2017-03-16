using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Data;
using System.Web.Mvc;

using Owin;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using System.Security.Claims;
using LD.IdentityManagement.API.Models;
using LD.IdentityManagement.API.Providers;

[assembly: Microsoft.Owin.OwinStartup(typeof(LD.IdentityManagement.API.Startup))]
namespace LD.IdentityManagement.API
{
	public partial class Startup
	{
		public static string[] Schema_private = System.Configuration.ConfigurationManager.AppSettings["Schema_private"].Split(',');

		public static Dictionary<string, LD.IdentityManagement.API.Models.Attribute> Schema_Attributes;
		public static Dictionary<string, LD.IdentityManagement.API.Models.Attribute> Schema_Private_Attributes;
		public static Dictionary<string, LD.IdentityManagement.API.Models.classobject> Schema_Object;
		public static Dictionary<Guid, LD.IdentityManagement.API.Models.managementagent> Schema_managementagent;

		private static System.Xml.XmlDocument mv_schema_xml;
		private static System.Xml.XmlDocument import_attribute_flow_xml;

		public static NLog.Logger logger;
		public static NLog.Logger accesslog;

		private System.Timers.Timer ReloadTime;
		void ReloadTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			DateTime startLoad = DateTime.Now;

			System.Xml.XmlDocument mv_schema_xml_temp = new System.Xml.XmlDocument();
			System.Xml.XmlDocument import_attribute_flow_xml_temp = new System.Xml.XmlDocument();
			System.Data.DataTable mms_management_agent_temp = new System.Data.DataTable("mms_metaverse_multivalue");
			System.Data.DataTable mms_metaverse_temp = new System.Data.DataTable("mms_metaverse");

			System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.AppSettings["FIMSynchronizationService"]);
			connection.Open();

			//Load XML schema
			//mv_schema_xml
            System.Data.SqlClient.SqlCommand SqlCommand = new System.Data.SqlClient.SqlCommand("select mv_schema_xml from mms_server_configuration (nolock)", connection);
			System.Xml.XmlReader XmlReader = SqlCommand.ExecuteXmlReader();
			mv_schema_xml_temp.Load(XmlReader);
			XmlReader.Dispose();
			SqlCommand.Dispose();

			//import_attribute_flow_xml
            SqlCommand = new System.Data.SqlClient.SqlCommand("select import_attribute_flow_xml from mms_server_configuration (nolock)", connection);
			XmlReader = SqlCommand.ExecuteXmlReader();
			import_attribute_flow_xml_temp.Load(XmlReader);
			XmlReader.Dispose();
			SqlCommand.Dispose();

			//
			////Load Tabell schema
			//SqlCommand = new System.Data.SqlClient.SqlCommand("select COLUMN_NAME from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = 'mms_metaverse'", connection);
			//System.Data.DataTable INFORMATION_SCHEMA = new System.Data.DataTable("INFORMATION_SCHEMA");
			//System.Data.SqlClient.SqlDataAdapter DataAdapter = new System.Data.SqlClient.SqlDataAdapter(SqlCommand);
			//int count = DataAdapter.Fill(INFORMATION_SCHEMA);
			//DataAdapter.Dispose();
			//SqlCommand.Dispose();

			System.Data.SqlClient.SqlDataAdapter DataAdapter;
			int count;

			//Load mms_metaverse attributes
            SqlCommand = new System.Data.SqlClient.SqlCommand("select top 1 * from mms_metaverse (nolock)", connection);
			DataAdapter = new System.Data.SqlClient.SqlDataAdapter(SqlCommand);
			count = DataAdapter.Fill(mms_metaverse_temp);
			DataAdapter.Dispose();
			SqlCommand.Dispose();

			//Load mms_management_agent
            SqlCommand = new System.Data.SqlClient.SqlCommand("select * from mms_management_agent (nolock)", connection);
			DataAdapter = new System.Data.SqlClient.SqlDataAdapter(SqlCommand);
			count = DataAdapter.Fill(mms_management_agent_temp);
			DataAdapter.Dispose();
			SqlCommand.Dispose();

			connection.Close();
			connection.Dispose();

			mv_schema_xml = mv_schema_xml_temp;
			import_attribute_flow_xml = import_attribute_flow_xml_temp;

			//mms_management_agent
            Dictionary<Guid, LD.IdentityManagement.API.Models.managementagent> Schema_managementagent_temp = new Dictionary<Guid, LD.IdentityManagement.API.Models.managementagent>();
			foreach (DataRow row in mms_management_agent_temp.Rows)
			{
				LD.IdentityManagement.API.Models.managementagent ma = new LD.IdentityManagement.API.Models.managementagent();
				ma.name = (string)row["ma_name"];
				ma.guid = (Guid)row["ma_id"];
				ma.description = (string)row["ma_description"];
				ma.type = (string)row["ma_type"];

				Schema_managementagent_temp.Add(ma.guid, ma);

				//import-flow
                foreach (System.Xml.XmlNodeList flowrule in import_attribute_flow_xml.SelectNodes(string.Format("/import-attribute-flow/import-flow-set/import-flows/import-flow[@src-ma='{0}']", ma.guid)))
				{
					flowObject temp = new flowObject()
					{
						type = "",
                        mvclassobjectname = "",
                        csclassobjectname = "",
                        attribute = new flowAttribute[0]
					};
				}
			}

			Schema_managementagent = Schema_managementagent_temp;

			System.Xml.XmlNamespaceManager ns = new System.Xml.XmlNamespaceManager(mv_schema_xml.NameTable);
			ns.AddNamespace("dsml", "http://www.dsml.org/DSML");
			//Create Schema Attributes list
            Dictionary<string, LD.IdentityManagement.API.Models.Attribute> Schema_Attributes_temp = new Dictionary<string, LD.IdentityManagement.API.Models.Attribute>();
			Dictionary<string, LD.IdentityManagement.API.Models.Attribute> Schema_Private_Attributes_temp = new Dictionary<string, LD.IdentityManagement.API.Models.Attribute>();
			foreach (System.Xml.XmlNode Node in mv_schema_xml.SelectNodes("dsml:dsml/dsml:directory-schema/dsml:attribute-type", ns))
			{
				LD.IdentityManagement.API.Models.Attribute NewAttribute = new LD.IdentityManagement.API.Models.Attribute();
				NewAttribute.name = Node["dsml:name"].InnerText.ToLower();
				NewAttribute.mulitvalue = Node.Attributes["single-value"] != null ? false : true;
				NewAttribute.indexable = Node.Attributes["ms-dsml:indexable"] == null || Node.Attributes["ms-dsml:indexable"].Value == "false" ? false : true;
				NewAttribute.indexed = Node.Attributes["ms-dsml:indexed"] == null || Node.Attributes["ms-dsml:indexed"].Value == "false" ? false : true;
				NewAttribute.syntax = Node["dsml:syntax"].InnerText;

				//syntax to type
                switch (NewAttribute.syntax)
				{
					case "1.3.6.1.4.1.1466.115.121.1.5":
					//Binary
                        NewAttribute.type = typeof(byte[]);
					break;
					case "1.3.6.1.4.1.1466.115.121.1.7":
					//Boolean
                        NewAttribute.type = typeof(bool);
					break;
					case "1.3.6.1.4.1.1466.115.121.1.12":
					//DN (string)
					NewAttribute.type = typeof(string);
					break;
					case "1.3.6.1.4.1.1466.115.121.1.15":
					//DirectoryString
                        NewAttribute.type = typeof(string);
					break;
					case "1.3.6.1.4.1.1466.115.121.1.27":
					//Integer
                        NewAttribute.type = typeof(int);
					break;
					default:
					//NewAttribute.type = typeof(string);
					break;
				}

				if (!Schema_private.Contains(NewAttribute.name))
				{
					if (NewAttribute.mulitvalue || mms_metaverse_temp.Columns.Contains(NewAttribute.name))
					{
						//logger.Debug("{0} {1}", NewAttribute.name, NewAttribute.mulitvalue);
						Schema_Attributes_temp.Add(NewAttribute.name, NewAttribute);
					}
				}
				else
				{
					Schema_Private_Attributes_temp.Add(NewAttribute.name, NewAttribute);
				}
			}

			Schema_Attributes = Schema_Attributes_temp;
			Schema_Private_Attributes = Schema_Private_Attributes_temp;

			//Create Schema Object list
            Dictionary<string, LD.IdentityManagement.API.Models.classobject> Schema_Object_temp = new Dictionary<string, LD.IdentityManagement.API.Models.classobject>();
			foreach (System.Xml.XmlNode Node in mv_schema_xml.SelectNodes("dsml:dsml/dsml:directory-schema/dsml:class", ns))
			{
				LD.IdentityManagement.API.Models.classobject newClassObject = new LD.IdentityManagement.API.Models.classobject();
				newClassObject.name = Node["dsml:name"].InnerText.ToLower();

				List<LD.IdentityManagement.API.Models.objectattribute> ObjectAttributeList = new List<LD.IdentityManagement.API.Models.objectattribute>();
				foreach (System.Xml.XmlNode attributeNodes in Node.SelectNodes("dsml:attribute", ns))
				{
					LD.IdentityManagement.API.Models.objectattribute NewObjectAttribute = new LD.IdentityManagement.API.Models.objectattribute();
					NewObjectAttribute.required = attributeNodes.Attributes["required"].Value == "true" ? true : false;
					LD.IdentityManagement.API.Models.Attribute Attribute;
					string attname = attributeNodes.Attributes["ref"].Value.Substring(1);
					if (Schema_Attributes.TryGetValue(attname, out Attribute) || Schema_Private_Attributes.TryGetValue(attname, out Attribute))
					{
						NewObjectAttribute.attribute = Attribute;
						ObjectAttributeList.Add(NewObjectAttribute);
					}
				}

				newClassObject.objectattributes = ObjectAttributeList.ToArray();
				Schema_Object_temp.Add(newClassObject.name, newClassObject);
			}

			Schema_Object = Schema_Object_temp;

			TimeSpan time = DateTime.Now - startLoad;
			ReloadTime.Enabled = true;
		}

		public void Configuration(IAppBuilder app)
		{
			// Token Generation
            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
			{
				AllowInsecureHttp = true,
                TokenEndpointPath = new Microsoft.Owin.PathString("/api/login/token"),
				//AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
				//AccessTokenExpireTimeSpan = TimeSpan.FromHours(1),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(int.Parse(System.Configuration.ConfigurationManager.AppSettings["AccessTokenExpireMin"])),
                Provider = new SimpleAuthorizationServerProvider()
			});
			app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

			HttpConfiguration httpConfiguration = new HttpConfiguration();
			WebApiConfig.Register(httpConfiguration);
			app.UseWebApi(httpConfiguration);

			//logg
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(System.Configuration.ConfigurationManager.AppSettings["LoggerConfig"]);
			string loggerFullName = typeof(LD.IdentityManagement.API.Startup).FullName;
			logger = NLog.LogManager.GetLogger(loggerFullName.Substring(0, loggerFullName.LastIndexOf('.')));
			accesslog = NLog.LogManager.GetLogger(loggerFullName.Substring(0, loggerFullName.LastIndexOf('.')) + ".accesslog");

			ReloadTime = new System.Timers.Timer(int.Parse(System.Configuration.ConfigurationManager.AppSettings["reloadtimeMin"]) * 60 * 1000);
			//ReloadTime.AutoReset = true;
			ReloadTime.Elapsed += ReloadTime_Elapsed;
			ReloadTime_Elapsed(null, null);
		}
	}
}