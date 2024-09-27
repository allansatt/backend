using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Constructs;

namespace PersonalBackend
{
        public class UserPoolStack : Stack{
            internal UserPoolStack(Construct scope, string id, StackProps props) : base(scope, id, props)
            {
                IPublicHostedZone hostedZone = PublicHostedZone.FromPublicHostedZoneAttributes(this, "HostedZone", new PublicHostedZoneAttributes
                {
                    HostedZoneId = "Z05029891PQ9DA7LFJP73",
                    ZoneName = "allansattelbergrivera.com",
                });
                // Create a dummy A record for the hosted zone
                new ARecord(this, "DummyARecord", new ARecordProps
                {
                    Zone = hostedZone,
                    RecordName = hostedZone.ZoneName,
                    Target = RecordTarget.FromIpAddresses("192.0.2.1") // Using a reserved IP for documentation
                });
                UserPool userPool = new UserPool(this, "UserPool", new UserPoolProps{
                    SelfSignUpEnabled= false,
                    Mfa = Mfa.REQUIRED,
                    Email = UserPoolEmail.WithSES(new UserPoolSESOptions{
                        FromEmail = $"no-reply@{hostedZone.ZoneName}",
                        ReplyTo = $"no-reply@{hostedZone.ZoneName}",
                        FromName = "Allan Satteberg Rivera",
                        SesVerifiedDomain = hostedZone.ZoneName
                    }),
                    StandardAttributes = new StandardAttributes{
                        Email = new StandardAttribute{
                            Required = true,
                            Mutable = false
                        }
                    },
                    MfaSecondFactor = new MfaSecondFactor{
                        Sms = true,
                        Otp = true,
                        Email = true
                    },
                    AutoVerify = new AutoVerifiedAttrs{
                        Email = true,
                        Phone = true
                    },
                    FeaturePlan = FeaturePlan.PLUS,
                    DeviceTracking = new DeviceTracking{
                        ChallengeRequiredOnNewDevice = true,
                        DeviceOnlyRememberedOnUserPrompt = true
                    },
                    UserInvitation = new UserInvitationConfig{
                        EmailSubject = "You've been invited to Allan Satteberg Rivera's Website",
                        EmailBody = "OTP for {username} {####}",	
                        SmsMessage = "OTP for {username} {####}"
                    },
                });
                userPool.AddGroup("Admins", new UserPoolGroupOptions{
                    GroupName = "Admins",
                    Description = "Admins of the website",
                });
                Certificate userPoolDomainCertificate = new Certificate(this, "CertificateAuth", new CertificateProps{
                    DomainName = $"auth.{hostedZone.ZoneName}",
                    Validation = CertificateValidation.FromDns(hostedZone),
                });
                UserPoolDomain userPoolDomain = new UserPoolDomain(this, "UserPoolDomain", new UserPoolDomainProps{
                    UserPool = userPool,
                    CustomDomain = new CustomDomainOptions{
                        DomainName = $"auth.{hostedZone.ZoneName}",
                        Certificate = userPoolDomainCertificate,
                    },
                    ManagedLoginVersion = ManagedLoginVersion.NEWER_MANAGED_LOGIN,
                    
                });
                new ARecord(this, "ARecord", new ARecordProps{
                    Zone = hostedZone,
                    RecordName = $"auth.{hostedZone.ZoneName}",
                    Target = RecordTarget.FromAlias(new UserPoolDomainTarget(userPoolDomain)),
                });
                UserPoolClient client = new UserPoolClient(this, "FrontendClient", new UserPoolClientProps{
                    UserPool= userPool,
                    AuthSessionValidity= Duration.Minutes(3),
                    IdTokenValidity= Duration.Minutes(5),
                    RefreshTokenValidity= Duration.Minutes(60),
                    AccessTokenValidity= Duration.Minutes(5),
                    SupportedIdentityProviders= [
                        UserPoolClientIdentityProvider.COGNITO,
                    ],
                    OAuth= new OAuthSettings{
                        CallbackUrls= ["https://fe.allansattelbergrivera.com/"],
                        LogoutUrls= ["https://fe.allansattelbergrivera.com/"],
                    },
                });
                new CfnManagedLoginBranding(this, "ManagedLoginBranding", new CfnManagedLoginBrandingProps{
                    UserPoolId = userPool.UserPoolId,
                    ClientId = client.UserPoolClientId,
                    ReturnMergedResources = true,
                    UseCognitoProvidedValues = true
                });
            }
        }
}