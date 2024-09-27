using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
namespace PersonalBackend
{
    public class ReactDeployStack : Stack
    {
        internal ReactDeployStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
        {
            Bucket deployBucket = new Bucket(this, "react-bucket", new BucketProps
            {
                BucketName = "allansatt-react",
                BlockPublicAccess= BlockPublicAccess.BLOCK_ALL,
                Encryption = BucketEncryption.S3_MANAGED,
                Versioned = true,
                RemovalPolicy = RemovalPolicy.DESTROY
            });
            Bucket loggingBucket = new Bucket(this, "logging-bucket", new BucketProps
            {
                BucketName = "allansatt-react-logging",
                BlockPublicAccess= BlockPublicAccess.BLOCK_ALL,
                Encryption = BucketEncryption.S3_MANAGED,
                Versioned = true,
                RemovalPolicy = RemovalPolicy.DESTROY
            });
            OriginAccessIdentity oai = new OriginAccessIdentity(this, "oai", new OriginAccessIdentityProps
            {
                Comment = "CloudFront Origin Access Identity for SPA"
            });
            deployBucket.GrantRead(oai);
            ICertificate certificate = Certificate.FromCertificateArn(this, "ACM_CERTIFICATE", $"arn:aws:acm:us-east-1:{props.Env.Account}:certificate/e4f08e15-788f-4bdd-931c-73d026f9eee8");
            new DomainName_(this, "DomainName", new DomainNameProps
            {
                DomainName = "fe.allansattelbergrivera.com",
                Certificate = certificate
            });

            Distribution distribution = new Distribution(this, "react-distribution", new DistributionProps
            {
                Certificate = certificate,
                DomainNames = new string[] { "fe.allansattelbergrivera.com" },
                DefaultRootObject = "index.html",
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = S3BucketOrigin.WithOriginAccessIdentity(deployBucket, new S3BucketOriginWithOAIProps{
                        OriginAccessIdentity = oai
                    }),
                },
            });

            loggingBucket.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Actions = new string[] { "s3:PutObject" },
                Resources = new string[] { loggingBucket.ArnForObjects("*") },
                Principals = new IPrincipal[] { new ServicePrincipal("logging.s3.amazonaws.com") },
                Conditions = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "StringEquals", new System.Collections.Generic.Dictionary<string, string> { { "aws:SourceAccount", props.Env.Account } } }
                } 
            }));

            IHostedZone hostedZone = HostedZone.FromHostedZoneAttributes(this, "HostedZone", new HostedZoneAttributes
            {
                HostedZoneId = "Z05029891PQ9DA7LFJP73",
                ZoneName = "allansattelbergrivera.com"
            });

            new Route53RecordTarget( new ARecord(this, "ARecord", new ARecordProps{
                Zone = hostedZone,
                RecordName = "fe",
                Target = RecordTarget.FromAlias(new CloudFrontTarget(distribution))
                })
            );
            

        }
    }
}