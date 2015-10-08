﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Interfaces.DataTransferObjects;

namespace pluginSlack.Interfaces
{
    public interface ISlackIntegration
    {
        string CreateAuthUrl(string externalStateToken);
        Task<string> GetOAuthToken(string code);
        Task<string> GetUserId(string oauthToken);
        Task<List<FieldDTO>> GetChannelList(string oauthToken);
    }
}
