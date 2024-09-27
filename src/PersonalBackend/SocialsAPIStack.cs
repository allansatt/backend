using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SSM;
using Constructs;
using APIResource = Amazon.CDK.AWS.APIGateway.Resource;

namespace PersonalBackend
{
    public class SocialsAPIStack : Stack
    {
        internal SocialsAPIStack(Construct scope, string id, ApiImplementationProps props) : base(scope, id, props){
            
            RestApi baseAPI = props.baseAPI;
            StringParameter socialsParameter = new StringParameter(
                this, "socials-parameter",
                new StringParameterProps{
                    ParameterName = "socials",
                    StringValue = "{\"linkedin\":\"https://www.linkedin.com/in/allansattelbergrivera/\",\"github\":\"https://github.com/allansatt\"}"
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
                            },
                            ResponseParameters = new System.Collections.Generic.Dictionary<string, string>
                            {
                                { "method.response.header.Access-Control-Allow-Origin", "'https://fe.allansattelbergrivera.com'" },
                                { "method.response.header.Access-Control-Allow-Headers", "'Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token'" },
                                { "method.response.header.Access-Control-Allow-Methods", "'GET,OPTIONS'" }
                            }
                        }
                    }
                }
            });

            // Add a resource to our API and attach an integration to it.
            APIResource socialsResource = baseAPI.Root.AddResource("socials");
            socialsResource.AddMethod("GET", socialsIntegration, new MethodOptions
            {
                MethodResponses = new MethodResponse[]
                {
                    new MethodResponse
                    {
                        StatusCode = "200",
                        ResponseParameters = new System.Collections.Generic.Dictionary<string, bool>
                        {
                            { "method.response.header.Access-Control-Allow-Origin", true },
                            { "method.response.header.Access-Control-Allow-Headers", true },
                            { "method.response.header.Access-Control-Allow-Methods", true }
                        }
                    }
                }
            });
        }
    }
}