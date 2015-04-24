﻿using System;
using System.IO;
using System.Threading;
using WesthillMail.SendCampaignWithAttachment.ApiServiceReference;

namespace WesthillMail.SendCampaignWithAttachment
{
    class Program
    {
        private const string UserName = "{ Your username here }";
        private const string Password = "{ Your password here }";

        private static ApiServiceClient _client;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += PrintUnhandledException;
            
            _client = GetWcfClient();

            ApiDocumentFolder folder = CreateDocumentFolder();
            ApiDocument document = UploadDocument(folder.Id);

            ApiCampaign campaign = CreateCampaign();
            AttachDocument(campaign, document);

            ApiAddressBook addressBook = CreateAddressBook();
            AddContactToAddressBook(addressBook);

            ApiCampaignSend sendResult = SendCampaignToAddressBook(campaign, addressBook);
            WaitUntilSendFinishes(sendResult);

            PrintCampaingSummary(campaign);
        }

        private static void PrintCampaingSummary(ApiCampaign campaign)
        {
            ApiCampaignSummary summary = _client.GetCampaignSummary(campaign.Id);
            Console.WriteLine("Campaign has been sended {0} times", summary.NumSent);
        }

        protected static void WaitUntilSendFinishes(ApiCampaignSend campaignSend)
        {
            ApiCampaignSendStatuses status = campaignSend.Status;
            while (status == ApiCampaignSendStatuses.NotSent ||
                   status == ApiCampaignSendStatuses.Sending ||
                   status == ApiCampaignSendStatuses.Scheduled)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));

                ApiCampaignSend progress = _client.GetCampaignSendProgress(campaignSend.Id);
                status = progress.Status;
            }
        }

        private static ApiCampaignSend SendCampaignToAddressBook(ApiCampaign campaign, ApiAddressBook addressBook)
        {
            ApiCampaignSend campaignSend = new ApiCampaignSend
            {
                CampaignId = campaign.Id,
                AddressBookIds = new[] { addressBook.Id }
            };

            ApiCampaignSend sendResult = _client.SendCampaign(campaignSend);
            Console.WriteLine("Campaign '{0}' has been sended to address book '{1}'", campaign.Name, addressBook.Name);
            return sendResult;
        }

        private static void AttachDocument(ApiCampaign campaign, ApiDocument document)
        {
            _client.AttachDocumentToCampaign(campaign.Id, document);
        }

        private static ApiDocument UploadDocument(int documentFolderId)
        {
            const String fileName = "SampleDocument.txt";
            byte[] data = File.ReadAllBytes(fileName);
            ApiDocument uploadedDocument = _client.UploadDocument(documentFolderId, fileName, data);

            return uploadedDocument;
        }

        private static ApiDocumentFolder CreateDocumentFolder()
        {
            ApiDocumentFolder documentFolder = new ApiDocumentFolder
            {
                Name = "Attachments",
            };
            ApiDocumentFolder result = _client.CreateDocumentFolder(0, documentFolder);
            return result;
        }

        private static ApiAddressBook CreateAddressBook()
        {
            ApiAddressBook addressBook = new ApiAddressBook
            {
                Name = Guid.NewGuid().ToString()
            };

            ApiAddressBook createdAddressBook = _client.CreateAddressBook(addressBook);
            Console.WriteLine("Address book '{0}' has been created", createdAddressBook.Name);
            return createdAddressBook;
        }

        private static void AddContactToAddressBook(ApiAddressBook addressBook)
        {
            ApiContact contact = new ApiContact
            {
                Email = string.Format("email{0}@example.com", Guid.NewGuid()),
            };

            ApiContact addedContact = _client.AddContactToAddressBook(addressBook.Id, contact);
            Console.WriteLine("Contact '{0}' has been added to address book '{1}'", addedContact.Email, addressBook.Name);
        }

        private static ApiCampaign CreateCampaign()
        {
            ApiCampaign campaign = new ApiCampaign
            {
                Name = "My campaign",
                Subject = "Subject",
                FromName = "Friendly name",
                HtmlContent = "<a href=\"http://$UNSUB$\">Unsubscribe</a>",
                PlainTextContent = "Unsubscribe $UNSUB$"
            };

            ApiCampaign createdCampaign = _client.CreateCampaign(campaign);
            Console.WriteLine("Campaign '{0}' has been created", createdCampaign.Name);
            return createdCampaign;
        }

        private static void PrintUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject);
        }

        private static ApiServiceClient GetWcfClient()
        {
            ApiServiceClient client = new ApiServiceClient("Secure_ApiService");
            client.ClientCredentials.UserName.UserName = UserName;
            client.ClientCredentials.UserName.Password = Password;
            return client;
        }
    }
}
