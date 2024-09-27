using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.CertificateManager;
using Constructs;
using Amazon.CDK.AWS.Route53;
namespace PersonalBackend
{
    class ApiImplementationProps : StackProps {
        public RestApi baseAPI;

    }
    public class BaseAPIStack : Stack
    {
        public readonly RestApi baseAPI;

        internal BaseAPIStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Get the domain name we want to use
            DomainName_ domainName = new DomainName_(this, "DomainName", new DomainNameProps
            {
                DomainName = "api.allansattelbergrivera.com",
                Certificate = Certificate.FromCertificateArn(this, "ACM_CERTIFICATE", $"arn:aws:acm:us-east-1:{props.Env.Account}:certificate/e4f08e15-788f-4bdd-931c-73d026f9eee8")
            });

            IHostedZone hostedZone = HostedZone.FromHostedZoneAttributes(this, "HostedZone", new HostedZoneAttributes
            {
                HostedZoneId = "Z05029891PQ9DA7LFJP73",
                ZoneName = "allansattelbergrivera.com"
            });
            new CnameRecord(this, "ApiGatewayRecordSet", new CnameRecordProps
            {
                Zone= hostedZone,
                RecordName= "api",
                DomainName= domainName.DomainNameAliasDomainName
            });

            this.baseAPI = new RestApi(this, "base-api", new RestApiProps
            {
                RestApiName = "Base API",
                Description = "API for allansattelbergrivera.com",
                DefaultCorsPreflightOptions= new CorsOptions{
                    AllowOrigins = new string[] { "https://fe.allansattelbergrivera.com" },
                    AllowMethods = new string[] { "*" },
                    AllowHeaders = new string[] { "*" }
                }
            });

            new BasePathMapping(this, "BasePathMapping", new BasePathMappingProps
            {
                DomainName = domainName,
                RestApi = baseAPI
            });
            baseAPI.AddUsagePlan("UsagePlan", new UsagePlanProps
            {   
                Name = "BaseAPIUsagePlan",
                Throttle = new ThrottleSettings
                {
                    RateLimit = 100,
                    BurstLimit = 2
                },
            });
        }
    }
}
