﻿namespace AspNetIdentity.WebApi
{
    using System;

    using AspNetIdentity.WebApi.Identity.Providers;
    using AspNetIdentity.WebApi.Models;
    using AspNetIdentity.WebApi.Models.Auth.Identity;

    using Autofac;

    using Microsoft.AspNet.Identity;
    using Microsoft.Owin;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.Cookies;
    using Microsoft.Owin.Security.DataHandler;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using Microsoft.Owin.Security.DataProtection;
    using Microsoft.Owin.Security.Infrastructure;
    using Microsoft.Owin.Security.Jwt;
    using Microsoft.Owin.Security.OAuth;

    using Owin;

    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app, IContainer container)
        {
            // Configure the db context and user manager to use a single instance per request
            app.CreatePerOwinContext(XAppDbContext.Create);
            app.CreatePerOwinContext<XUserManager>(XUserManager.Create);
            app.CreatePerOwinContext<XRoleManager>(XRoleManager.Create);

            ConfigureOAuthTokenGeneration(app, container);

            //// Api controllers with an [Authorize] attribute will be validated with JWT
            //TODO: Make it working: 
            //ConfigureOAuthTokenConsumption(app, container);

            //// These two lines (app.UseCookieAuthentication and app.UseExternalSignInCookie) allows to use
            //// this.Request.GetOwinContext().GetUserManager<XUserManager>() inside ApiControllers
            // app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                AuthenticationMode = AuthenticationMode.Active,
                ExpireTimeSpan = TimeSpan.FromDays(20),
                CookieSecure = CookieSecureOption.SameAsRequest,
                CookieName = CookieAuthenticationDefaults.CookiePrefix + "XAspNetIdentity",
                SlidingExpiration = true
            });
        }

        private static void ConfigureOAuthTokenGeneration(IAppBuilder app, IContainer container)
        {
            // OAuth 2.0 Bearer Access Token Generation
            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
//For Dev enviroment only (on production should be AllowInsecureHttp = false)
#if DEBUG
                AllowInsecureHttp = true,
#endif
                TokenEndpointPath = new PathString("/auth/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromDays(1),
                AuthorizationCodeExpireTimeSpan = TimeSpan.FromDays(1),
                Provider = container.Resolve<IOAuthAuthorizationServerProvider>(),
                AccessTokenFormat = new CustomJwtFormat(AppSettings.AuthCustomJwtFormat),
                RefreshTokenFormat = new TicketDataFormat(app.CreateDataProtector(typeof(OAuthAuthorizationServerMiddleware).Namespace, "Refresh_Token", "v1")),
                RefreshTokenProvider = container.Resolve<IAuthenticationTokenProvider>()
            });
        }

        private static void ConfigureOAuthTokenConsumption(IAppBuilder app, IContainer container)
        {
            var issuer = AppSettings.AuthCustomJwtFormat;
            string audienceId = AudiencesStore.DefaultAudience.AudienceId;
            byte[] audienceSecret = TextEncodings.Base64Url.Decode(AudiencesStore.DefaultAudience.Base64Secret);

            // Api controllers with an [Authorize] attribute will be validated with JWT
            app.UseJwtBearerAuthentication(
            new JwtBearerAuthenticationOptions
            {
                AuthenticationMode = AuthenticationMode.Active,
                AllowedAudiences = new[] { audienceId },
                IssuerSecurityTokenProviders = new IIssuerSecurityTokenProvider[]
                {
                    new SymmetricKeyIssuerSecurityTokenProvider(issuer, audienceSecret)
                }
            });
        }
    }
}
