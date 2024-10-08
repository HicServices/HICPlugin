using HIC.Common.InterfaceToJira.JIRA;
using HIC.Common.InterfaceToJira.JIRA.RestApiClient2.JiraModel;
using RestSharp;

namespace InterfaceToJira.RestApiClient2
{
    public static class RESTHelper
    {

        public static bool Execute(IRestClient client, RestRequest request)
        {
            var restResponse = client.Execute(request);
            if (restResponse.ResponseStatus != ResponseStatus.Completed || restResponse.StatusCode.IsError() || restResponse.ErrorException != null)
                throw new JiraApiException(
                    $"RestSharp response status: {restResponse.ResponseStatus} - HTTP response: {restResponse.StatusCode} - {restResponse.StatusDescription} - {restResponse.Content}", restResponse.ErrorException);
            return true;
        }

        public static T Execute<T>(IRestClient client, RestRequest request) where T : new()
        {
            var restResponse = client.Execute<T>(request);
            if (restResponse.ResponseStatus != ResponseStatus.Completed || restResponse.StatusCode.IsError() || restResponse.ErrorException != null)
                throw new JiraApiException(
                    $"RestSharp response status: {restResponse.ResponseStatus} - HTTP response: {restResponse.StatusCode} - {restResponse.StatusDescription} - {restResponse.Content}", restResponse.ErrorException);

            return restResponse.Data;
        }
    }
}
