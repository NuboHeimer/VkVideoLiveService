using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

///----------------------------------------------------------------------------
///   Module:       VKPlay Live Service
///   Author:       play_code (https://twitch.tv/play_code)
///   Email:        info@play-code.live
///   WebSite:		https://docs.play-code.live/
///----------------------------------------------------------------------------

///----------------------------------------------------------------------------
///   Module:       Fix for GetRandomViewer, GetNewViewers, RewardsManager
///   Author:       NuboHeimer (https://live.vkplay.ru/nuboheimer)
///   Email:        nuboheimer@yandex.ru
///----------------------------------------------------------------------------

///   Version:      2.1.1

public class CPHInline
{
    private readonly HttpClient Client = new();
    private Logger Logger;
    private VKPlayApiService Service;

    public void Init()
    {
        Logger = new Logger(CPH, "-- VKPlay Service:");
        Service = new VKPlayApiService(Client, Logger);
        if (CPH.GetGlobalVar<List<string>>("vkplay_todays_viewers", true) == null) {
            CPH.SetGlobalVar("vkplay_todays_viewers", new List<string>(), true);
            CPH.LogInfo("Создана глобальная переменная vkplay_todays_viewers");
        }
    }

    public bool ClearTodaysViewers()
    {
        CPH.SetGlobalVar("vkplay_todays_viewers", new List<string>(), true);
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
            CPH.LogInfo("[VkPlay reward manager] Reward with id " + rewardId + " enabled");
        }
        catch (Exception e)
        {
            Logger.Error("Error enabling reward with id " + rewardId, e.Message);
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
            CPH.LogInfo("[VkPlay reward manager] Reward with id " + rewardId + " disabled");
        }
        catch (Exception e)
        {
            Logger.Error("Error disabling reward with id " + rewardId, e.Message);
        }

        return true;
    }

    public bool GetViewers()
    {
        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        try
        {
            var viewers = Service.GetViewers(channelName);
            for (int i = 0; i < viewers.Count; i++)
            {
                CPH.SetArgument(string.Format("viewer{0}", i), viewers[i].DisplayName);
            }
        }
        catch (Exception e)
        {
            Logger.Error("Error fetching viewers list", e.Message);
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
            Logger.Error("Error fetching viewers count", e.Message);
            return false;
        }

        return true;
    }

    public bool GetNewViewers()
    {

        if (!args.ContainsKey("channel_name"))
            return false;
        string channelName = args["channel_name"].ToString();
        List<string> vkplay_todays_viewers = CPH.GetGlobalVar<List<string>>("vkplay_todays_viewers", true);
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
                if (!vkplay_todays_viewers.Contains(viewers[i].DisplayName))
                {
                    vkplay_todays_viewers.Add(viewers[i].DisplayName);
                    CPH.SetGlobalVar("vkplay_todays_viewers", vkplay_todays_viewers, true);
                    CPH.SetArgument("service", "VKPlay");
                    CPH.SetArgument("title", "Новый зритель");
                    CPH.SetArgument("message", viewers[i].DisplayName);
                    CPH.ExecuteMethod("MiniChat Method Collection", "CreateCustomEvent");
                    CPH.LogInfo("Новый зритель: " + viewers[i].DisplayName);
                    Thread.Sleep(200);
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
        List<string> vkplay_todays_viewers = CPH.GetGlobalVar<List<string>>("vkplay_todays_viewers", true);
        vkplay_todays_viewers.Add(args["userName"].ToString());
        CPH.SetGlobalVar("vkplay_todays_viewers", vkplay_todays_viewers, true);
        return true;
    }
}

public class VKPlayApiService
{
    private HttpClient Client { get; set; }
    private Logger Logger { get; set; }

    private const string ServiceApiHost = "https://api.live.vkplay.ru/v1";
    private const string EndpointTplGetUserData = "/blog/{0}/public_video_stream/chat/user/";
    private const string EndpointSetRewardState = "/channel/{0}/manage/point/reward/{1}/enabled";
    public VKPlayApiService(HttpClient client, Logger logger)
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