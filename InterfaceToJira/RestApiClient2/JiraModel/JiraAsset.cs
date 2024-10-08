using System;
using System.Collections.Generic;
using System.Text;

namespace InterfaceToJira.RestApiClient2.JiraModel
{
    public class JiraAsset
    {

        public string workspaceId { get; set; }
        public int id { get; set; }
        public string label { get; set; }
        public string objectKey { get; set; }
    }
}
