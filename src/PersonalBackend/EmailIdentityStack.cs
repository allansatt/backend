using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.SES;
using Constructs;

namespace PersonalBackend
{
    public class EmailIdentityStack : Stack
    {
        internal EmailIdentityStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            IPublicHostedZone hostedZone = PublicHostedZone.FromPublicHostedZoneAttributes(this, "HostedZone", new PublicHostedZoneAttributes
            {
                HostedZoneId = "Z05029891PQ9DA7LFJP73",
                ZoneName = "allansattelbergrivera.com",
            });
            new EmailIdentity(this, "EmailIdentity", new EmailIdentityProps{
                    Identity= Identity.PublicHostedZone(hostedZone),
                    MailFromDomain= "login.allansattelbergrivera.com",
                }
            );

        }
    }
}