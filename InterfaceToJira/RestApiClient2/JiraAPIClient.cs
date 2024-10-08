using Azure;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2;
using InterfaceToJira.RestApiClient2.JiraModel;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace InterfaceToJira.RestApiClient2;

class Workspace
{
    public string workspaceId { get; set; }
}

public class JiraAPIClient
{

    private readonly RestClient client;

    private readonly string _workspaceID;


    public JiraAPIClient(JiraAccount account)
    {
        client = new RestClient(new RestClientOptions("https://api.atlassian.com")
        {
            Authenticator = new HttpBasicAuthenticator(account.User, account.Password)
        });


        var jiraClient = new RestClient(new RestClientOptions(account.ServerUrl)
        {
            Authenticator = new HttpBasicAuthenticator(account.User, account.Password)
        });
        var response = jiraClient.Execute(new RestRequest
        {
            Resource = "/rest/servicedeskapi/assets/workspace",
            Method = Method.Get
        });
        if (response.IsSuccessful)
        {
            var workspace = JsonConvert.DeserializeObject<Workspace>(response.Content);
            _workspaceID = workspace.workspaceId;
        }
        else
        {
            throw new Exception(response.ErrorMessage);
        }
    }

    public List<JiraAsset> GetAllProjectAssets()
    {

        var request = new RestRequest
        {
            Resource = $"/jsm/assets/workspace/{_workspaceID}/v1/object/aql",
            Method = Method.Post,
            RequestFormat = DataFormat.Json,
        };
        request.AddBody(new AQLQuery("objectType = Project"));
        return RESTHelper.Execute<List<JiraAsset>>(client, request);
    }
}
