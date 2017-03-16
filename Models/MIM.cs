using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace MIM.Models
{
    public class Attribute
    {
        public string name { get; set; }
        public bool mulitvalue { get; set; }
        public bool indexable { get; set; }
        public bool indexed { get; set; }
        public string syntax { get; set; }
        public Type type { get; set; }
    }

    public class objectattribute
    {
        public bool required { get; set; }
        public Attribute attribute { get; set; }
    }

    public class classobject
    {
        public string name { get; set; }
        public objectattribute[] objectattributes { get; set; }
    }

    public class managementagent
    {
        public string name { get; set; }
        public Guid guid { get; set; }
        public string type { get; set; }
        public int export_type { get; set; }
        public int capabilities { get; set; }
        public string description { get; set; }
        public string assembly { get; set; }
        public string deprovisionering { get; set; }
        public bool enablerecall { get; set; }
        public flowObject[] flowObjects { get; set; }
        public string[] csattribute { get; set; }
    }

    public class flowObject
    {
        public string type { get; set; }
        public string mv { get; set; }
        public string cs { get; set; }
        public flowAttribute[] attribute { get; set; }
    }

    public class flowAttribute
    {
        public int order { get; set; }
        public int direction { get; set; }
        public string[] cs { get; set; }
        public string[] mv { get; set; }
        public string rulename { get; set; }
        public bool allownull { get; set; }
    }

    public class mimobject
    {
        public Guid object_id { get; set; }
        public string object_type { get; set; }
        public DateTime last_modification_date { get; set; }
        public Dictionary<string, string[]> attributes { get; set; }
    }
    public class connectorspace : mimobject
    {
        public string ma_name { get; set; }
        public Guid ma_id { get; set; }
        public string rdn { get; set; }
        public DateTime last_import_modification_date { get; set; }
        public DateTime last_export_modification_date { get; set; }
        public XmlDocument data { get; set; }
    }
    public class connectorspacehistory
    {
        public connectorspace connectorspace { get; set; }
        public connectorspace[] history { get; set; }
    }

    public class connectedma
    {
        public string ma_name { get; set; }
        public Guid ma_id { get; set; }
        public List<Guid> connectorobject { get; set; }
    }

    public class metaverse : mimobject
    {
        public List<connectedma> connectedma { get; set; }
    }

    public class stats
    {
        public int resualtcount { get; set; }
        public TimeSpan exectime { get; set; }
        public DateTime starttime { get; set; }
        public DateTime endtime { get; set; }

        public Dictionary<string, string> info { get; set; }
    }

}