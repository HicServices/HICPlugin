// Decompiled with JetBrains decompiler
// Type: HIC.Common.InterfaceToJira.JIRA.Extensions
// Assembly: HIC.Common.InterfaceToJira, Version=1.1.5098.0, Culture=neutral, PublicKeyToken=null
// MVID: 00D725ED-BB48-409E-9D4B-D6FB0DC12FE2
// Assembly location: C:\Users\AzureUser_JS\.nuget\packages\interfacetojira\1.1.5098\lib\net35\HIC.Common.InterfaceToJira.dll

using System.Net;

namespace HIC.Common.InterfaceToJira.JIRA;

public static class Extensions
{
    public static bool IsError(this HttpStatusCode code) => code == HttpStatusCode.BadRequest || code == HttpStatusCode.Unauthorized || code == HttpStatusCode.PaymentRequired || code == HttpStatusCode.Forbidden || code == HttpStatusCode.NotFound || code == HttpStatusCode.MethodNotAllowed || code == HttpStatusCode.NotAcceptable || code == HttpStatusCode.ProxyAuthenticationRequired || code == HttpStatusCode.RequestTimeout || code == HttpStatusCode.Conflict || code == HttpStatusCode.Gone || code == HttpStatusCode.LengthRequired || code == HttpStatusCode.PreconditionFailed || code == HttpStatusCode.RequestEntityTooLarge || code == HttpStatusCode.RequestUriTooLong || code == HttpStatusCode.UnsupportedMediaType || code == HttpStatusCode.RequestedRangeNotSatisfiable || code == HttpStatusCode.ExpectationFailed || code == HttpStatusCode.InternalServerError || code == HttpStatusCode.NotImplemented || code == HttpStatusCode.BadGateway || code == HttpStatusCode.ServiceUnavailable || code == HttpStatusCode.GatewayTimeout || code == HttpStatusCode.HttpVersionNotSupported;
}