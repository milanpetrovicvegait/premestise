﻿using Core.Interfaces.Models;
using Microsoft.Extensions.Configuration;
using Persistence.Interfaces.Contracts;
using Persistence.Interfaces.Entites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Util.Enums;
using RazorEngine;
using RazorEngine.Templating;
using Core.Services;
using System.Linq;

namespace Core.Clients
{
    public interface IMailClient
    {
        void Send(string fromEmail, string message);
        void Send(string fromEmail, List<AlternateView> altViews, IEnumerable<string> ccEmails = null);
        void SendVerificationMessage(RequestDto request, KindergardenDto fromKindergarden, IEnumerable<KindergardenDto> wishes);
        void SendFoundMatchMessage(RequestDto firstMatch, RequestDto secondMatch, KindergardenDto from, KindergardenDto to);
        void SendCircularMatchMessage(List<MatchedRequest> validChain);
    }

    public class MailClient : IMailClient
    {
        public const string Subject = "Nova poruka od premesti.se";

        private readonly string _defaultEmail;
        private readonly string _environment;
        private readonly string _ccEmail;
        private readonly ISmtpClientFactory _smtpClientFactory;
        private readonly IKindergardenRepository _kindergardenRepository;
        private readonly EmailTemplateService template = new EmailTemplateService();

        private const string _unmatchPageUrl = "placeholder";
        private const string _verificationPageUrl = "placeholder";
        private const string _infoNotValidPageUrl = "placeholder";
        private const string _confirmMatchPageUrl = "placeholder";

        // ovo negde u config ili nesto
        private readonly string _circularTemplatePath = $"{Environment.CurrentDirectory}\\AppData\\circular.htm";
        private readonly string _verificationTemplatePath = $"{Environment.CurrentDirectory}\\AppData\\verification.htm";
        private readonly string _matchTemplatePath = $"{Environment.CurrentDirectory}\\AppData\\index.htm";
        private readonly string _bannerPath = $"{Environment.CurrentDirectory}\\AppData\\images\\top-banner.jpg";
        private readonly string _footerPath = $"{Environment.CurrentDirectory}\\AppData\\images\\logo-footer.png";
        private readonly string _parentTemplatePath = $"{Directory.GetParent(Environment.CurrentDirectory)}\\Core\\Templates\\parent.htm";

        public MailClient(ISmtpClientFactory smtpClientFactory, IConfiguration config, IKindergardenRepository kindergardenRepository)
        {
            _smtpClientFactory = smtpClientFactory;
            _kindergardenRepository = kindergardenRepository;
            _defaultEmail = config.GetSection("DefaultEmail").Value;
            _environment = config.GetSection("env").Value;
            _ccEmail = config.GetSection("DSIEmail").Value;
        }

        public void Send(string toEmail, string message)
        {
            using (var smtpClient = _smtpClientFactory.CreateDefaultClient())
            {
                MailAddress receiverEmail = new MailAddress(toEmail);

                using (MailMessage mail = new MailMessage(new MailAddress(_defaultEmail), receiverEmail))
                {
                    mail.Subject = Subject;
                    mail.Body = message;
                    mail.IsBodyHtml = true;
                    smtpClient.Send(mail);
                }
            }
        }

        public void Send(string toEmail, List<AlternateView> altViews, IEnumerable<string> ccEmails = null)
        {
            ccEmails = ccEmails ?? Enumerable.Empty<string>();

            using (var smtpClient = _smtpClientFactory.CreateDefaultClient())
            {
                MailAddress receiverEmail = new MailAddress(toEmail);

                using (MailMessage mail = new MailMessage(new MailAddress(_defaultEmail), receiverEmail))
                {
                    mail.Subject = Subject;
                    foreach (AlternateView altView in altViews)
                    {
                        mail.AlternateViews.Add(altView);
                    }
                    foreach (string cc in ccEmails)
                    {
                        mail.CC.Add(cc);
                    }
                    mail.IsBodyHtml = true;
                    smtpClient.Send(mail);
                }
            }
        }

        /// <summary>
        /// Sends email for verification with given request information
        /// </summary>
        /// <param name="request">Parent RequestDto object</param>
        /// <param name="fromKindergarden">KindergardenDto of the current kindergarden</param>
        /// <param name="wishes">List of requested kindergarden wishes</param>
        public void SendVerificationMessage(RequestDto request, KindergardenDto fromKindergarden, IEnumerable<KindergardenDto> wishes)
        {
            // Ako postoji drugi nacin da se slika stavi u email - izmenite
            // pokusao sam img src="http://localhost:50800/assets/images/..." ali nece u mail da stavi

            using (StreamReader reader = new StreamReader(_verificationTemplatePath))
            {
                string mailText = reader.ReadToEnd();
                var groupMapper = new AgeGroupMapper();
                mailText = mailText.Replace("[[PARENT_NAME]]", request.ParentName);
                mailText = mailText.Replace("[[CHILD_GROUP]]", groupMapper.mapGroupToText(request.Group));
                mailText = mailText.Replace("[[PHONE_NUMBER]]", request.PhoneNumber);
                mailText = mailText.Replace("[[EMAIL]]", request.Email);
                mailText = mailText.Replace("[[URL_ENV]]", _environment);
                mailText = mailText.Replace("[[FROM_KINDERGARDEN]]", $"{fromKindergarden.Name}");
                mailText = mailText.Replace("[[HASHED_ID]]", request.Id);

                StringBuilder toKindergardensBuilder = new StringBuilder();
                foreach (KindergardenDto wish in wishes)
                    toKindergardensBuilder.Append($"{wish.Name}<br>");
                mailText = mailText.Replace("[[TO_KINDERGARDENS]]", toKindergardensBuilder.ToString());

                AlternateView bannerImageAltView = new AlternateView(_bannerPath, MediaTypeNames.Image.Jpeg);
                AlternateView footerImageAltView = new AlternateView(_footerPath, MediaTypeNames.Image.Jpeg);
                bannerImageAltView.TransferEncoding = TransferEncoding.Base64;
                footerImageAltView.TransferEncoding = TransferEncoding.Base64;

                mailText = mailText.Replace("[[TOP_BANNER_LOGO_SRC]]", $"cid:{bannerImageAltView.ContentId}");
                mailText = mailText.Replace("[[FOOTER_LOGO_SRC]]", $"cid:{footerImageAltView.ContentId}");
                AlternateView messageAltView = AlternateView.CreateAlternateViewFromString(mailText, null, MediaTypeNames.Text.Html);

                Send(request.Email, new List<AlternateView> { messageAltView, bannerImageAltView, footerImageAltView });
            }
        }

        /// <summary>
        /// Sends email to both parents with given match information
        /// </summary>
        /// <param name="firstMatch">RequestDto object of first matched parent</param>
        /// <param name="secondMatch">RequestDto object of second matched parent</param>
        /// <param name="from">KindergardentDto object of first matched parent</param>
        /// <param name="to">KindergardenDto object of second matched parent</param>
        public void SendFoundMatchMessage(RequestDto firstMatch, RequestDto secondMatch, KindergardenDto from, KindergardenDto to)
        {
            using (StreamReader reader = new StreamReader(_matchTemplatePath))
            {
                string mailText = reader.ReadToEnd();

                List<AlternateView> firstParentMailViews = CreateMatchMail(mailText, firstMatch, secondMatch, from, to);
                List<AlternateView> secondParentMailViews = CreateMatchMail(mailText, secondMatch, firstMatch, to, from);

                Send(firstMatch.Email, firstParentMailViews);
                Send(secondMatch.Email, secondParentMailViews);
            }
        }

        // DRY Helper Method
        private List<AlternateView> CreateMatchMail(string mail, RequestDto firstMatch, RequestDto secondMatch, KindergardenDto from, KindergardenDto to)
        {
            // images
            AlternateView bannerImageAltView = new AlternateView(_bannerPath, MediaTypeNames.Image.Jpeg);
            AlternateView footerImageAltView = new AlternateView(_footerPath, MediaTypeNames.Image.Jpeg);
            bannerImageAltView.TransferEncoding = TransferEncoding.Base64;
            footerImageAltView.TransferEncoding = TransferEncoding.Base64;

            // mail
            mail = mail.Replace("[[PARENT_NAME]]", firstMatch.ParentName);
            mail = mail.Replace("[[FROM_KINDERGARDEN]]", from.Name);
            mail = mail.Replace("[[TO_KINDERGARDEN]]", to.Name);
            mail = mail.Replace("[[MATCH_PARENT_NAME]]", secondMatch.ParentName);
            mail = mail.Replace("[[MATCH_PHONE]]", secondMatch.PhoneNumber);
            mail = mail.Replace("[[MATCH_EMAIL]]", secondMatch.Email);
            mail = mail.Replace("[[TOP_BANNER_LOGO_SRC]]", $"cid:{bannerImageAltView.ContentId}");
            mail = mail.Replace("[[FOOTER_LOGO_SRC]]", $"cid:{footerImageAltView.ContentId}");
            mail = mail.Replace("[[MATCHED_REQUEST_ID]]", firstMatch.Id);
            mail = mail.Replace("[[URL_ENV]]", _environment);

            List<AlternateView> mailViews = new List<AlternateView>()
            {
                // image alternate views
                bannerImageAltView,
                footerImageAltView,

                // mail alternate view
                AlternateView.CreateAlternateViewFromString(mail, null, MediaTypeNames.Text.Html)
            };

            return mailViews;
        }

        public void SendCircularMatchMessage(List<MatchedRequest> validChain)
        {
            List <Kindergarden> fromRequestsKindergardens = new List<Kindergarden>(validChain.Count);
            //popuni listu, iz svakog zahteva iz lanca izvuci odakle se zeli premestaj sto ce biti dovoljno za email
            foreach (MatchedRequest request in validChain)
            {
                fromRequestsKindergardens.Add(_kindergardenRepository.GetById(request.FromKindergardenId));
            }

            // Uncomment in case we have to send emails to kindergardens and get Email property
            //IEnumerable<string> distinctEmails = fromRequestsKindergardens.Select(x => x.Email).Distinct();
            var groupMapper = new AgeGroupMapper();
            string ageGroup = groupMapper.mapGroupToText(validChain[0].Group);
            
            List<MatchInformation> matches = new List<MatchInformation>();
            for (var i = 0; i < validChain.Count; i++)
            {
                MatchInformation current = new MatchInformation();
                current.Name = validChain[i].ParentName;
                current.Email = validChain[i].ParentEmail;
                current.Phone = validChain[i].ParentPhoneNumber;
                current.FromKindergarden = fromRequestsKindergardens[i].Name;

                if ( i < validChain.Count -1)
                {   
                    current.ToKindergarden = fromRequestsKindergardens[i + 1].Name;
                } else 
                {
                    current.ToKindergarden = fromRequestsKindergardens[0].Name;
                }
                matches.Add(current);
            }

            // Uncomment in case we have to send emails to kindergardens
            // List<MatchEmailInformation> emailInformationForKindergarden = GetEmailInformationForKindergardens(validChain, matches, distinctEmails, ageGroup);
            // SendEmailsToKindergardens(emailsForKindergarden);

            List<MatchEmailInformation> emailInformationForParents = GetEmailInformationForParents(validChain, matches, ageGroup);
            SendEmailsToAllParents(emailInformationForParents);
        }


        private List<MatchEmailInformation> GetEmailInformationForKindergardens(
            List<MatchedRequest> validChain,
            List<MatchInformation> matches,
            IEnumerable<string> distinctEmails,
            string ageGroup)
        {
            List <MatchEmailInformation> emailsForKindergarden = new List<MatchEmailInformation>();
            foreach (string kinderGardenEmail in distinctEmails)
            {
                MatchEmailInformation current = new MatchEmailInformation();
                current.AgeGroup = ageGroup;
                current.ChainLength = validChain.Count;
                current.ToEmail = kinderGardenEmail;
                current.Matches = matches;
                emailsForKindergarden.Add(current);
            }

            return emailsForKindergarden;
        }

        private List<MatchEmailInformation> GetEmailInformationForParents(
            List<MatchedRequest> validChain,
            List<MatchInformation> matches,
            string ageGroup)
        {
            List<MatchEmailInformation> emailsForParents = new List<MatchEmailInformation>();
            foreach (MatchedRequest request in validChain)
            {
                MatchEmailInformation current = new MatchEmailInformation();
                current.AgeGroup = ageGroup;
                current.ChainLength = validChain.Count;
                current.ToEmail = request.ParentEmail;
                current.Matches = matches;
                emailsForParents.Add(current);
            }
            return emailsForParents;
        }

        private void SendEmailsToKindergardens2(List<MatchEmailInformation> emailsForKindergarden)
        {
            AlternateView bannerImageAltView = new AlternateView(_bannerPath, MediaTypeNames.Image.Jpeg);
            AlternateView footerImageAltView = new AlternateView(_footerPath, MediaTypeNames.Image.Jpeg);
            bannerImageAltView.TransferEncoding = TransferEncoding.Base64;
            footerImageAltView.TransferEncoding = TransferEncoding.Base64;
            foreach (MatchEmailInformation info in emailsForKindergarden)
            {
                using (StreamReader reader = new StreamReader(_circularTemplatePath))
                {
                    string mailText = reader.ReadToEnd();
                    info.TopBannerLogo = $"cid:{bannerImageAltView.ContentId}";
                    info.FooterLogo = $"cid:{footerImageAltView.ContentId}";

                    var result = Engine.Razor.RunCompile(mailText, Guid.NewGuid().ToString(), typeof(MatchEmailInformation), info);
                    AlternateView messageAltView = AlternateView.CreateAlternateViewFromString(result, null, MediaTypeNames.Text.Html);
                    Send(info.ToEmail, new List<AlternateView> { messageAltView, bannerImageAltView, footerImageAltView });
                }
            }
        }

        private void SendEmailsToAllParents(List<MatchEmailInformation> emails)
        {
            foreach (MatchEmailInformation info in emails)
            {
                using (StreamReader reader = new StreamReader(_circularTemplatePath))
                {
                    string mailText = reader.ReadToEnd();

                    AlternateView bannerImageAltView = new AlternateView(_bannerPath, MediaTypeNames.Image.Jpeg);
                    AlternateView footerImageAltView = new AlternateView(_footerPath, MediaTypeNames.Image.Jpeg);
                    bannerImageAltView.TransferEncoding = TransferEncoding.Base64;
                    footerImageAltView.TransferEncoding = TransferEncoding.Base64;
                    info.TopBannerLogo = $"cid:{bannerImageAltView.ContentId}";
                    info.FooterLogo = $"cid:{footerImageAltView.ContentId}";

                    var result = Engine.Razor.RunCompile(mailText, Guid.NewGuid().ToString(), typeof(MatchEmailInformation), info);
                    AlternateView messageAltView = AlternateView.CreateAlternateViewFromString(result, null, MediaTypeNames.Text.Html);
                    Send(info.ToEmail, new List<AlternateView> { messageAltView, bannerImageAltView, footerImageAltView });
                }
            }
        }

    }
}
