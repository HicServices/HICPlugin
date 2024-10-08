using System;
using System.Collections.Generic;
using System.Text;

namespace InterfaceToJira.RestApiClient2.JiraModel
{
    public class AQLQuery
    {
        string qlQuery { get; set; }

        public AQLQuery(string query) { qlQuery = query; }
    }
}
