﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Adliance.AspNetCore.Buddy.Abstractions;
using Mailjet.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Resources = Mailjet.Client.Resources;

namespace Adliance.AspNetCore.Buddy.Email.Mailjet
{
    public class MailjetHealthCheck : IHealthCheck
    {
        private readonly IMailjetConfiguration _mailjetConfiguration;
        private readonly IEmailConfiguration _emailConfiguration;
        private readonly ILogger<MailjetHealthCheck> _logger;

        public MailjetHealthCheck(IMailjetConfiguration mailjetConfiguration, IEmailConfiguration emailConfiguration, ILogger<MailjetHealthCheck> logger)
        {
            _mailjetConfiguration = mailjetConfiguration;
            _emailConfiguration = emailConfiguration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthy = false;
            try
            {
                var client = new MailjetClient(_mailjetConfiguration.PublicApiKey, _mailjetConfiguration.PrivateApiKey)
                {
                    Version = ApiVersion.V3_1
                };

                var request = new MailjetRequest
                    {
                        Resource = Resources.Send.Resource
                    }
                    .Property("SandboxMode", true)
                    .Property(Resources.Send.Messages, new JArray
                    {
                        new JObject
                        {
                            {
                                "From", new JObject
                                {
                                    {"Email", _emailConfiguration.EmailSenderAddress},
                                    {"Name", _emailConfiguration.EmailSenderName}
                                }
                            },
                            {
                                "To", new JArray
                                {
                                    new JObject
                                    {
                                        {"Email", "hannes@sachsenhofer.com"}
                                    }
                                }
                            },
                            {
                                "ReplyTo", string.IsNullOrWhiteSpace(_emailConfiguration.EmailReplyToAddress)
                                    ? null
                                    : new JObject
                                    {
                                        {"Email", _emailConfiguration.EmailReplyToAddress},
                                        {"Name", _emailConfiguration.EmailSenderName}
                                    }
                            },
                            {"Subject", "Health Check"},
                            {"TextPart", "Health Check"}
                        }
                    });
                MailjetResponse response = await client.PostAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    healthy = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mailjet health check failed.");
            }

            if (healthy)
            {
                return await Task.FromResult(HealthCheckResult.Healthy("Mailjet is healthy."));
            }

            return await Task.FromResult(HealthCheckResult.Unhealthy("MailJet is not healthy."));
        }
    }
}