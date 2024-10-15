using Azure;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2;
using InterfaceToJira.RestApiClient2;
using InterfaceToJira.RestApiClient2.JiraModel;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace InterfaceToJira.RestApiClient2;

class Value
{
    public string workspaceId { get; set; }
}

class Workspace
{
    public int size { get; set; }
    public int start { get; set; }
    public int limit { get; set; }
    public bool isLastPage { get; set; }
    public IList<Value> values { get; set; }
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
            _workspaceID = workspace.values.First().workspaceId;
        }
        else
        {
            throw new Exception(response.ErrorMessage);
        }
    }

    public JiraModel.AssetResponse GetProjectAsset(int id)
    {
        var request = new RestRequest
        {
            Resource = $"/jsm/assets/workspace/{_workspaceID}/v1/object/{id}",
            Method = Method.Get,
        };
        var response = RESTHelper.Execute<AssetResponse>(client, request);
        return response;
    }

    public List<JiraAsset> GetAllProjectAssets()
    {
        List<JiraAsset> assets = new();
        var request = new RestRequest
        {
            Resource = $"/jsm/assets/workspace/{_workspaceID}/v1/object/aql",
            Method = Method.Post,
            RequestFormat = DataFormat.Json,
        };
        request.AddBody("{\"qlQuery\": \"objectType = Project\"}");

        var response = RESTHelper.Execute<AQLResponse>(client, request);
        int total = response.total;
        assets.AddRange(response.values);
        while(total > assets.Count) {
            //go get the rest
            request = new RestRequest
            {
                Resource = $"/jsm/assets/workspace/{_workspaceID}/v1/object/aql?startAt={assets.Count+1}",
                Method = Method.Post,
                RequestFormat = DataFormat.Json,
            };
            request.AddBody("{\"qlQuery\": \"objectType = Project\"}");

            var additioanlResponse = RESTHelper.Execute<AQLResponse>(client, request);
            assets.AddRange(additioanlResponse.values);
        }
        return assets.ToList();
    }
}
