﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Fr8.Infrastructure.Data.Control;
using Fr8.Infrastructure.Data.Crates;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Managers;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.Infrastructure.Data.States;
using Fr8.TerminalBase.BaseClasses;
using Fr8.TerminalBase.Infrastructure;
using Fr8.TerminalBase.Services;
using PhoneNumbers;
using Twilio;
using terminalUtilities.Twilio;


namespace terminalTwilio.Activities
{
    public class Send_Via_Twilio_v1 : TerminalActivity<Send_Via_Twilio_v1.ActivityUi>
    {
        public const string MessageCrageLabel = "Message Data";

        public static ActivityTemplateDTO ActivityTemplateDTO = new ActivityTemplateDTO
        {
            Id = new Guid("ddd5be71-a23c-41e3-baf0-501e34f0517b"),
            Name = "Send_Via_Twilio",
            Label = "Send SMS Using Twilio Account",
            Tags = "Twillio,Notifier",
            Version = "1",
            MinPaneWidth = 330,
            Terminal = TerminalData.TerminalDTO,
            Categories = new[]
            {
                ActivityCategories.Forward,
                TerminalData.ActivityCategoryDTO
            }
        };

        public class ActivityUi : StandardConfigurationControlsCM
        {
            public TextSource SmsNumber { get; set; }

            public TextSource SmsBody { get; set; }

            public ActivityUi(UiBuilder uiBuilder)
            {
                SmsNumber = uiBuilder.CreateSpecificOrUpstreamValueChooser("SMS Number", "SMS_Number", addRequestConfigEvent: true, requestUpstream: true, availability: AvailabilityType.RunTime);
                Controls.Add(SmsNumber);
                SmsBody = uiBuilder.CreateSpecificOrUpstreamValueChooser("SMS Body", "SMS_Body", addRequestConfigEvent: true, requestUpstream: true, availability: AvailabilityType.RunTime);
                Controls.Add(SmsBody);
            }
        }
        protected override ActivityTemplateDTO MyTemplate => ActivityTemplateDTO;

        private readonly ITwilioService _twilioService;

        public Send_Via_Twilio_v1(ICrateManager crateManager, ITwilioService twilioServiceService)
            : base(crateManager)
        {
            _twilioService = twilioServiceService;
        }

        public override Task Initialize()
        {
            //No extra configuration is required, only signal runtime crates
            CrateSignaller.MarkAvailableAtRuntime<StandardPayloadDataCM>(MessageCrageLabel, true)
                          .AddFields("Status", "ErrorMessage", "Body", "ToNumber");
            return Task.FromResult(0);
        }

        public override Task FollowUp()
        {
            //No extra configuration is required
            return Task.FromResult(0);
        }

        public override async Task Run()
        {
            var smsNumber = GeneralizePhoneNumber(ActivityUI.SmsNumber.TextValue);
            var smsBody = $"{ActivityUI.SmsBody.TextValue}\nThis message was generated by Fr8. http://www.fr8.co";
            var message = _twilioService.SendSms(smsNumber, smsBody);
            Payload.Add(Crate<StandardPayloadDataCM>.FromContent(MessageCrageLabel, ConvertMessageToPayload(message)));
        }

        private StandardPayloadDataCM ConvertMessageToPayload(Message message)
        {
            return new StandardPayloadDataCM(
                new KeyValueDTO("Status", message.Status),
                new KeyValueDTO("ErrorMessage", message.ErrorMessage),
                new KeyValueDTO("Body", message.Body),
                new KeyValueDTO("ToNumber", message.To));
        }

        protected override Task Validate()
        {
            ValidationManager.ValidateTextSourceNotEmpty(ActivityUI.SmsBody, "SMS text can't be empty");
            if (ValidationManager.ValidateTextSourceNotEmpty(ActivityUI.SmsNumber, "SMS number can't be empty"))
            {
                ValidationManager.ValidatePhoneNumber(ActivityUI.SmsNumber.TextValue, ActivityUI.SmsNumber);
            }
            return base.Validate();
        }

        private string GeneralizePhoneNumber(string smsNumber)
        {
            var phoneUtil = PhoneNumberUtil.GetInstance();
            smsNumber = new string(smsNumber.Where(s => char.IsDigit(s) || s == '+' || (phoneUtil.IsAlphaNumber(smsNumber) && char.IsLetter(s))).ToArray());
            if (smsNumber.Length == 10 && !smsNumber.Contains("+"))
            {
                //we assume that default region is USA
                smsNumber = "+1" + smsNumber;
            }
            return smsNumber;
        }
    }
}