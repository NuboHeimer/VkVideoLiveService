///----------------------------------------------------------------------------
///   Module:       VK Video Live Service
///   Author:       play_code (https://twitch.tv/play_code)
///   Refactored:   NuboHeimer (https://live.vkvideo.ru/nuboheimer)
///   Email:        info@play-code.live
///   WebSite:		https://docs.play-code.ru/minichat
///----------------------------------------------------------------------------
 
///----------------------------------------------------------------------------
///   Module:       GetNewViewers, RewardsManager, GetSeasonStatistics
///   Author:       NuboHeimer (https://live.vkvideo.ru/nuboheimer)
///   Email:        nuboheimer@yandex.ru
///----------------------------------------------------------------------------
 
///   Version:      3.1.0
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private readonly HttpClient Client = new();
    private Logger Logger;
    private VKVideoLiveApiService Service;
    public void Init()
    {
        Logger = new Logger(CPH, "-- VKVideoLive Service:");
        Service = new VKVideoLiveApiService(Client, Logger);

        if (CPH.GetGlobalVar<List<string>>("vkvideolive_todays_viewers", true) == null)
        {
            CPH.SetGlobalVar("vkvideolive_todays_viewers", new List<string>(), true);
        }
    }

    public bool ClearTodaysViewers()
    {
        CPH.SetGlobalVar("vkvideolive_todays_viewers", new List<string>(), true);
        return true;
    }

    public bool OnReward()
    {
        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        string rewardId = args["rewardId"].ToString();
        string rewardState = "On";
        string token = args["token"].ToString();
        try
        {
            Service.ChangeRewardState(channelName, rewardId, rewardState, token);
            CPH.LogInfo("[VKVideoLive reward manager] Reward with id " + rewardId + " enabled");
        }
        catch (Exception e)
        {
            Logger.Error("[VKVideoLive reward manager] Error enabling reward with id " + rewardId, e.Message);
        }

        return true;
    }

    public bool OffReward()
    {
        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        string rewardId = args["rewardId"].ToString();
        string rewardState = "Off";
        string token = args["token"].ToString();
        try
        {
            Service.ChangeRewardState(channelName, rewardId, rewardState, token);
            CPH.LogInfo("[VKVideoLive reward manager] Reward with id " + rewardId + " disabled");
        }
        catch (Exception e)
        {
            Logger.Error("[VKVideoLive reward manager] Error disabling reward with id " + rewardId, e.Message);
        }

        return true;
    }

    public bool GetViewers()
    {
        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        List<Dictionary<string, object>> listOfViewers = new List<Dictionary<string, object>>();
        try
        {
            var viewers = Service.GetViewers(channelName);

            for (int i = 0; i < viewers.Count; i++)
            {
                CPH.SetArgument(string.Format("viewer{0}", i), viewers[i].DisplayName);

                Dictionary<string, object> user = new Dictionary<string, object>
                {
                    { "userName", viewers[i].DisplayName },
                    { "id", viewers[i].ID },
                };
                listOfViewers.Add(user);
            }

            CPH.SetArgument("users", listOfViewers);
        }
        catch (Exception e)
        {
            Logger.Error("[VKVideoLive get viewers] Error fetching viewers list", e.Message);
        }

        return true;
    }

    public bool GetRandomViewer()
    {
        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        string last_random_viewer = CPH.GetGlobalVar<String>("last_random_viewer", true);
        try
        {
            var viewers = Service.GetViewers(channelName);
            if (viewers.Count == 0)
            {
                CPH.SetArgument("viewer", "");
                return true;
            }

            var rnd = new Random();
            var viewer = viewers[rnd.Next(viewers.Count)];
            if (viewers.Count > 1)
            {
                while (viewer.DisplayName.Equals(last_random_viewer))
                {
                    viewers.Remove(viewer);
                    viewer = viewers[rnd.Next(viewers.Count)];
                }
            }

            CPH.SetArgument("viewer", viewer.DisplayName);
            CPH.SetGlobalVar("last_random_viewer", viewer.DisplayName, true);
        }
        catch (Exception e)
        {
            Logger.Error("Error fetching viewers list", e.Message);
        }

        return true;
    }

    public bool GetViewersCount()
    {
        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        try
        {
            CPH.SetArgument("viewers_count", Service.GetViewersCount(channelName));
        }
        catch (Exception e)
        {
            Logger.Error("[VKVideoLive get viewers count] Error fetching viewers count", e.Message);
            return false;
        }

        return true;
    }

    public bool GetNewViewers()
    {
        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        List<string> vkvideolive_todays_viewers = CPH.GetGlobalVar<List<string>>("vkvideolive_todays_viewers", true);
        try
        {
            CPH.LogInfo("Попытка получить нового зрителя на вкпл");
            var viewers = Service.GetViewers(channelName);
            if (viewers.Count == 0)
            {
                CPH.LogInfo("Список зрителей пуст");
                return true;
            }

            for (int i = 0; i < viewers.Count; i++)
            {
                if (!vkvideolive_todays_viewers.Contains(viewers[i].DisplayName))
                {
                    vkvideolive_todays_viewers.Add(viewers[i].DisplayName);
                    CPH.SetGlobalVar("vkvideolive_todays_viewers", vkvideolive_todays_viewers, true);
                    CPH.SetArgument("service", "VKVideoLive");
                    CPH.SetArgument("title", "Новый зритель");
                    CPH.SetArgument("message", viewers[i].DisplayName);
                    CPH.ExecuteMethod("MiniChat Method Collection", "CreateCustomEvent");
                    CPH.LogInfo("Новый зритель: " + viewers[i].DisplayName);
                    Thread.Sleep(200); // Без задержки лента миничата пропускает часть событий.
                }
            }
        }
        catch (Exception e)
        {
            Logger.Error("Error fetching new viewer", e.Message);
        }

        return true;
    }

    public bool AddFirstWordViewer()
    {
        List<string> vkvideolive_todays_viewers = CPH.GetGlobalVar<List<string>>("vkvideolive_todays_viewers", true);
        vkvideolive_todays_viewers.Add(args["userName"].ToString());
        CPH.SetGlobalVar("vkvideolive_todays_viewers", vkvideolive_todays_viewers, true);
        return true;
    }

    public bool GetTotalAverageViewrs()
    {
        string channelName = args["channel_name"].ToString();
        string token = args["token"].ToString();
        var json = Service.GetAllStatistics(channelName, token);
        JObject parsedJson = JObject.Parse(json);
        int totalAverageVKVideoLiveViewers = parsedJson["data"]["analytics"]["total"]["viewersAverage"].Value<int>();
        CPH.SetArgument("totalAverageVKVideoLiveViewers", totalAverageVKVideoLiveViewers);
        return true;
    }
}

public class VKVideoLiveApiService
{
    private HttpClient Client { get; set; }
    private Logger Logger { get; set; }

    private const string ServiceApiHost = "https://api.live.vkvideo.ru/v1";
    private const string EndpointTplGetUserData = "/blog/{0}/public_video_stream/chat/user/";
    private const string EndpointSetRewardState = "/channel/{0}/manage/point/reward/{1}/enabled";
    private const string EndpointGetSeasonStatistics = "/channel/{0}/support_program/season/{1}/statistic/{2}/daily/";
    private const string EndpointAllStatistics = "/channel/{0}/analytics?aggregate_interval=day&date_interval=30day";
    //    private const string EndpointGetSeasonDaysOnAir = "days_on_air/daily/";
    //    private const string EndpointGetSeasonRaidMembers = "raid_members/daily/";
    //    private const string EndpointGetSeasonViewTimes = "view_time/daily/";
    //    private const string EndpointGetSeasonChPRewardActivate = "cp_reward_activate/daily/";
    //    private const string EndpointGetSeasonLikes = "like/daily/";
    //    private const string EndpointGetSeasonTotalProfit= "total_profit/daily/";
    public VKVideoLiveApiService(HttpClient client, Logger logger)
    {
        Client = client;
        Logger = logger;
    }

    private UserInfoResponse GetUserDataAsync(string channelName)
    {
        string url = string.Format(ServiceApiHost + EndpointTplGetUserData, channelName);
        try
        {
            using HttpResponseMessage response = Client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonConvert.DeserializeObject<Dictionary<string, UserInfoResponse>>(responseBody)["data"] ?? null;
        }
        catch (HttpRequestException e)
        {
            Logger.Error("Error from client", e.Message);
            return null;
        }
    }

    public List<UserData> GetViewers(string channelName)
    {
        var userData = GetUserDataAsync(channelName);
        if (userData == null)
            return new();
        var users = userData.Users;
        foreach (var moderator in userData.Moderators)
            users.Add(moderator);
        return users;
    }

    public int GetViewersCount(string channelName)
    {
        var userData = GetUserDataAsync(channelName);
        if (userData == null)
            return 0;
        return userData.Count.users + userData.Count.moderators;
    }

    public void ChangeRewardState(string channelName, string rewardId, string rewardState, string token)
    {
        string url = string.Format(ServiceApiHost + EndpointSetRewardState, channelName, rewardId);
        string stub = "";
        string jsonString = JsonConvert.SerializeObject(stub);
        try
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using HttpContent httpContent = new StringContent(jsonString);
            if (rewardState == "On")
            {
                using HttpResponseMessage response = Client.PutAsync(url, httpContent).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            if (rewardState == "Off")
            {
                using HttpResponseMessage response = Client.DeleteAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }
        catch (HttpRequestException e)
        {
            Logger.Error("Error from client", e.Message);
        }
    }

    public string GetSeasonStatistics(string channelName, string seasonNumber, string requestType, string token)
    {
        string response = "stub";
        return response;
    }

    public string GetAllStatistics(string channelName, string token)
    {
        string url = string.Format(ServiceApiHost + EndpointAllStatistics, channelName);
        try
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using HttpResponseMessage response = Client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        catch (HttpRequestException e)
        {
            Logger.Error("Error from client", e.Message);
            return ("Error from client");
        }
    }

    public class UserInfoResponse
    {
        [JsonProperty("owner")]
        public UserData Owner { get; set; }

        [JsonProperty("moderators")]
        public List<UserData> Moderators { get; set; }

        [JsonProperty("users")]
        public List<UserData> Users { get; set; }

        [JsonProperty("count")]
        public UserInfoCounts Count { get; set; }
    }

    public class UserInfoCounts
    {
        public int permanentBans;
        public int moderators;
        public int temporaryBans;
        public int users;
    }

    public class UserData
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("avatarUrl")]
        public string AvatarUrl { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                {
                    "displayName",
                    DisplayName
                },
                {
                    "id",
                    ID.ToString()
                },
                {
                    "avatarUrl",
                    AvatarUrl
                },
            };
        }
    }
}

public class Logger
{
    private IInlineInvokeProxy cph { get; set; }
    private string Prefix { get; set; }

    public Logger(IInlineInvokeProxy _CPH, string prefix)
    {
        cph = _CPH;
        Prefix = prefix;
    }

    public void WebError(WebException e)
    {
        var response = (HttpWebResponse)e.Response;
        var statusCodeResponse = response.StatusCode;
        int statusCodeResponseAsInt = ((int)response.StatusCode);
        Error("WebException with status code " + statusCodeResponseAsInt.ToString(), statusCodeResponse);
    }

    public void Error(string message)
    {
        message = string.Format("{0} {1}", Prefix, message);
        cph.LogWarn(message);
    }

    public void Error(string message, params object[] additional)
    {
        string finalMessage = message;
        foreach (var line in additional)
        {
            finalMessage += ", " + line;
        }

        Error(finalMessage);
    }

    public void Debug(string message)
    {
        message = string.Format("{0} {1}", Prefix, message);
        cph.LogDebug(message);
    }

    public void Debug(string message, params object[] additional)
    {
        string finalMessage = message;
        foreach (var line in additional)
        {
            finalMessage += ", " + line;
        }

        Debug(finalMessage);
    }
}