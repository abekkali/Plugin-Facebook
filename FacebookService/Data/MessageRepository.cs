using Microsoft.Extensions.Caching.Memory;
using FacebookService.Models;
using System.Text.Json;

namespace FacebookService.Data
{
    public class MessageRepository : IMessageRepository
    {
        private static readonly string PageId = "106229432580358";// remplacer par id_Page
        private static readonly string AccessToken = "EAAIT9Ef1bPoBO3u3jzIYla4k4k0PzPJVzL4W4FFihKWAXbr6lEkxJhwVLL2vVFDJaBLo5SKJiGO4FBEVIB1MSa2pe9Jq5tVUcZCbt52ZAMnDpb8T5JADKXm0gaubCga8mK8C4buMUuUoMAoAhEUbdWW5QfwMWvr1DfbepzNVQ7N57U8kkCqxZAFUhQTRuiLJuSL12MZD"; //remplacer par Token
        private static readonly string BaseApiUrl = "https://graph.facebook.com/v17.0/";
        private readonly IMemoryCache _cache;

        public MessageRepository(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<List<Message>> FetchMessagesAsync()
        {
            List<Message> messages = new List<Message>();
            _cache.TryGetValue("LastCommentTime", out DateTime lastCommentTime);
            DateTime newLastCommentTime = lastCommentTime;

            using (HttpClient httpClient = new HttpClient())
            {
                var postsJson = await GetPostsJsonAsync(httpClient);
                foreach (var post in postsJson.GetProperty("data").EnumerateArray())
                {
                    var commentsJson = await GetCommentsJsonAsync(post, httpClient);
                    messages.AddRange(ProcessComments(commentsJson, ref newLastCommentTime, lastCommentTime));
                }

                _cache.Set("LastCommentTime", newLastCommentTime);
            }
            return messages;
        }

        private async Task<JsonElement> GetPostsJsonAsync(HttpClient httpClient)
        {
            var postsUrl = $"{BaseApiUrl}{PageId}/feed?fields=id&access_token={AccessToken}";
            var postsResponseMessage = await httpClient.GetAsync(postsUrl);
            HandleError(postsResponseMessage);
            var postsResponse = await postsResponseMessage.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(postsResponse);
        }

        private async Task<JsonElement> GetCommentsJsonAsync(JsonElement post, HttpClient httpClient)
        {
            var postId = post.GetProperty("id").GetString();
            var commentsUrl = $"{BaseApiUrl}{postId}/comments?fields=id,message,from,created_time,comments{{id,message,from,created_time}}&access_token={AccessToken}&order=reverse_chronological";
            var commentsResponse = await httpClient.GetStringAsync(commentsUrl);
            return JsonSerializer.Deserialize<JsonElement>(commentsResponse);
        }

        private List<Message> ProcessComments(JsonElement commentsJson, ref DateTime newLastCommentTime, DateTime lastCommentTime)
        {
            List<Message> messages = new List<Message>();
            foreach (var comment in commentsJson.GetProperty("data").EnumerateArray())
            {
                messages.AddRange(ProcessComment(comment, ref newLastCommentTime, lastCommentTime));
            }
            return messages;
        }

        private List<Message> ProcessComment(JsonElement comment, ref DateTime newLastCommentTime, DateTime lastCommentTime)
        {
            List<Message> messages = new List<Message>();

            var commentId = comment.GetProperty("id").GetString();
            var commentCreatedTime = DateTime.Parse(comment.GetProperty("created_time").GetString());
            var commentMessage = comment.GetProperty("message").GetString();
            string from = null;
            if (comment.TryGetProperty("from", out var fromProperty) && fromProperty.TryGetProperty("name", out var fromName))
            {
                from = fromName.GetString();
            }

            if (commentCreatedTime > lastCommentTime)
            {
                messages.Add(new Message { Id = commentId, Name = from, Text = commentMessage, Date = commentCreatedTime });
                if (commentCreatedTime > newLastCommentTime) newLastCommentTime = commentCreatedTime;
            }

            if (comment.TryGetProperty("comments", out var replies))
            {
                foreach (var reply in replies.GetProperty("data").EnumerateArray())
                {
                    var replyId = reply.GetProperty("id").GetString();
                    var replyCreatedTime = DateTime.Parse(reply.GetProperty("created_time").GetString());
                    if (replyCreatedTime > lastCommentTime)
                    {
                        var replyMessage = reply.GetProperty("message").GetString();
                        string replyFrom = null;
                        if (reply.TryGetProperty("from", out var replyFromProperty) && replyFromProperty.TryGetProperty("name", out var replyFromName))
                        {
                            replyFrom = replyFromName.GetString();
                        }

                        messages.Add(new Message { Id = replyId, ParentId = commentId, Name = replyFrom, Text = replyMessage, Date = replyCreatedTime });
                        if (replyCreatedTime > newLastCommentTime) newLastCommentTime = replyCreatedTime;
                    }
                }
            }

            return messages;
        }

        private void HandleError(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = response.Content.ReadAsStringAsync().Result;
                throw new Exception(errorContent);
            }
        }



    }
}