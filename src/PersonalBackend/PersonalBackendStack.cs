using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using APIResource = Amazon.CDK.AWS.APIGateway.Resource;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.CertificateManager;
using Constructs;
using Amazon.CDK.AWS.Route53;
namespace PersonalBackend
{
    public class PersonalBackendStack : Stack
    {
        internal PersonalBackendStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            StringParameter socialsParameter = new StringParameter(
                this, "socials-parameter",
                new StringParameterProps{
                    ParameterName = "socials",
                    StringValue = "{'linkedin':'https://www.linkedin.com/in/allansattelbergrivera/','github':'https://github.com/allansatt'}"
                }
            );
            Role readSocialsRole = new Role(this, "read-socials-role",new RoleProps{
                AssumedBy = new ServicePrincipal("apigateway.amazonaws.com")
            });
            socialsParameter.GrantRead(readSocialsRole);
            AwsIntegration socialsIntegration = new AwsIntegration(new AwsIntegrationProps
            {
                Service = "ssm",
                Action = "GetParameter",
                ActionParameters= new System.Collections.Generic.Dictionary<string, string>
                {
                    { "Name", "socials" }
                },
                IntegrationHttpMethod = "POST",
                Options = new IntegrationOptions
                {
                    CredentialsRole = readSocialsRole,
                    IntegrationResponses = new IntegrationResponse[]
                    {
                        new IntegrationResponse
                        {
                            StatusCode = "200",
                            ResponseTemplates = new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "application/json", "$input.path('$.GetParameterResponse.GetParameterResult.Parameter.Value')" }
                            }
                        }
                    }
                }
            });

            RestApi socialsApi = new RestApi(this, "socials-api", new RestApiProps
            {
                RestApiName = "Socials API",
                Description = "API for accessing social parameters"
            });

            // Add a resource to our API and attach an integration to it.
            APIResource socialsResource = socialsApi.Root.AddResource("socials");
            socialsResource.AddMethod("GET", socialsIntegration, new MethodOptions
            {
                MethodResponses = new MethodResponse[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200"
                    }
                }
            });
    	    // Get the domain name we want to use
            DomainName_ domainName = new DomainName_(this, "DomainName", new DomainNameProps
            {
                DomainName = "api.allansattelbergrivera.com",
                Certificate = Certificate.FromCertificateArn(this, "ACM_CERTIFICATE", $"arn:aws:acm:us-east-1:{props.Env.Account}:certificate/e4f08e15-788f-4bdd-931c-73d026f9eee8")
            });

            BasePathMapping basePathMapping = new BasePathMapping(this, "BasePathMapping", new BasePathMappingProps
            {
                DomainName = domainName,
                RestApi = socialsApi
            });

            IHostedZone hostedZone = HostedZone.FromHostedZoneAttributes(this, "HostedZone", new HostedZoneAttributes
            {
                HostedZoneId = "Z05029891PQ9DA7LFJP73",
                ZoneName = "allansattelbergrivera.com"
            });
            // Finally, add a CName record in the hosted zone with a value of the new custom domain that was created above:
            new CnameRecord(this, "ApiGatewayRecordSet", new CnameRecordProps
            {
                Zone= hostedZone,
                RecordName= "api",
                DomainName= domainName.DomainNameAliasDomainName
            });
        }
    }
}
