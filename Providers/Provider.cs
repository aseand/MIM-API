using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using LD.IdentityManagement.API.Models;
using System.DirectoryServices.AccountManagement;

namespace LD.IdentityManagement.API.Providers
{
	public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider
	{
		public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
		{
			Task task = Task.Factory.StartNew(() =>
         			{
         				// Resource owner password credentials does not provide a client ID.
                if (context.ClientId == null)
         				{
         					context.Validated();
         				}
         			});
			await task;
		}

		public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
		{
			//var cert = System.Web.HttpContext.Current.Request.ClientCertificate;
			//var User = System.Web.HttpContext.Current.User;

			context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });
			//Validate username and password
            Task task = Task.Factory.StartNew(() =>
         			{
         				using (PrincipalContext PrincipalContext = new PrincipalContext(ContextType.Domain, "DOMAIN.NAME"))
         				{
         					// validate the credentials
                    if (PrincipalContext.ValidateCredentials(context.UserName, context.Password))
         					{
         						UserPrincipal user = UserPrincipal.FindByIdentity(PrincipalContext, context.UserName);

         						LD.IdentityManagement.API.Startup.accesslog.Info("User {0} login from {1}", user.UserPrincipalName, context.Request.LocalIpAddress);
         						ClaimsIdentity ClaimsIdentity = new ClaimsIdentity(context.Options.AuthenticationType);
         						ClaimsIdentity.AddClaim(new Claim("http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider", "Active Directory"));
         						ClaimsIdentity.AddClaim(new Claim(ClaimTypes.Name, @"DOMAIN\" + user.SamAccountName));
         						ClaimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, @"DOMAIN\" + user.SamAccountName));
         						//ClaimsIdentity.AddClaim(new Claim("DistinguishedName", user.DistinguishedName));
         						ClaimsIdentity.AddClaim(new Claim(ClaimTypes.GivenName, user.GivenName));
         						ClaimsIdentity.AddClaim(new Claim(ClaimTypes.Surname, user.Surname));
         						ClaimsIdentity.AddClaim(new Claim("IpAddress", context.Request.LocalIpAddress));
         						foreach (Principal group in user.GetGroups())
         						{
         							ClaimsIdentity.AddClaim(new Claim(ClaimTypes.Role, @"DOMAIN\" + group.SamAccountName));
         						}

         						context.Validated(ClaimsIdentity);
         					}
         					else
         					{
         						LD.IdentityManagement.API.Startup.accesslog.Info("invalid login on {0} from {1}", context.UserName, context.Request.LocalIpAddress);
         						context.Rejected();
         						context.SetError("invalid_grant", "invalid_grant");
         					}
         				}
         			});
			await task;
		}
	}
}