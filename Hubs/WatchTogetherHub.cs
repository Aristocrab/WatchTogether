using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace WatchTogether.Hubs
{
    public class WatchTogether : Hub
    {
        public async Task AddToPlaylist(string videoId)
        {
            if (UsersList.Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                {
                    try
                    {
                        var group = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId).Group;
                        if (PlaylistList.Playlists.Any(p => p.Group == group))
                        {
                            PlaylistList.Playlists.First(p => p.Group == group).Videos.Add(videoId);
                            await Clients.Group(user.Group).SendAsync("AddToPlaylist", videoId);
                        }
                        else
                        {
                            PlaylistList.Playlists.Add(new Playlist {Group = group});
                            PlaylistList.Playlists.First(p => p.Group == group).Videos.Add(videoId);
                            await Clients.Group(user.Group).SendAsync("AddToPlaylist", videoId);
                        }

                        if (PlaylistList.Playlists.First(p => p.Group == group).Videos.Count == 1)
                            await Clients.Group(user.Group).SendAsync("ChangeVideo", videoId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Data);
                    }
                }
            }
        }

        public async Task NextInPlaylist()
        {
            if (UsersList.Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                {
                    var group = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId).Group;
                    if (PlaylistList.Playlists.First(p => p.Group == group).Videos.Any())
                    {
                        await Clients.Group(user.Group)
                            .SendAsync("ChangeVideo", PlaylistList.Playlists.First(p => p.Group == group).Videos[0]);
                    }
                }
            }
        }

        public async Task RemoveFromPlaylist(string id)
        {
            if (UsersList.Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                {
                    var group = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId).Group;
                    if (string.IsNullOrEmpty(id))
                    {
                        await Clients.Group(user.Group).SendAsync("RemoveFromPlaylist",
                            PlaylistList.Playlists.First(p => p.Group == group).Videos[0]);
                        PlaylistList.Playlists.First(p => p.Group == group).Videos.RemoveAt(0);
                    }
                    else
                    {
                        await Clients.Group(user.Group).SendAsync("RemoveFromPlaylist", id);
                        PlaylistList.Playlists.First(p => p.Group == group).Videos.Remove(id);
                    }
                }
            }
        }

        public async Task PlayVideo()
        {
            if (UsersList.Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                    await Clients.GroupExcept(user.Group, Context.ConnectionId).SendAsync("PlayVideo");
                else
                    await Clients.Caller.SendAsync("PauseVideo");
            }
        }

        public async Task PauseVideo()
        {
            if (UsersList.Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                    await Clients.GroupExcept(user.Group, Context.ConnectionId).SendAsync("PauseVideo");
                else
                    await Clients.Caller.SendAsync("PlayVideo");
            }
        }

        public async Task JoinGroup(string group)
        {
                await Groups.AddToGroupAsync(Context.ConnectionId, group);

                if (UsersList.Users.All(u => u.Group != group))
                {
                    UsersList.Users.Add(new User
                    {
                        ConnectionId = Context.ConnectionId,
                        Group = group,
                        Moderator = true
                    });
                    await Clients.Caller.SendAsync("SetGroup", $"Admin of {group}");
                    await Clients.Group(group).SendAsync("AddAdmin", Context.ConnectionId);
                }
                else
                {
                    UsersList.Users.Add(new User {ConnectionId = Context.ConnectionId, Group = group});
                    await Clients.Caller.SendAsync("SetGroup", $"User of {group}");
                    await Clients.Group(group).SendAsync("AddMember", Context.ConnectionId);
                }
        }

        public async Task ToggleModerator(string connectionId)
        {
            var user = UsersList.Users.Where(u => u.ConnectionId == connectionId).ToList();
            var caller = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId);
            
            if (user.Any() && caller.Moderator)
            {
                user.First().Moderator = !user.First().Moderator;
                await Clients.Group(user.First().Group)
                    .SendAsync("ToggleModerator", connectionId, user.First().Moderator);
            }
        }

        public async Task KickUser(string connectionId)
        {
            var user = UsersList.Users.Where(u => u.ConnectionId == connectionId).ToList();
            var caller = UsersList.Users.First(u => u.ConnectionId == Context.ConnectionId);

            if (user.Any() && caller.Moderator)
            {
                user.First().Moderator = false;
                await Clients.GroupExcept(caller.Group, user.First().ConnectionId).SendAsync("KickUser", connectionId);
                await Clients.Client(user.First().ConnectionId).SendAsync("Kick");
                UsersList.Users.Remove(user.First());
            }
        }
    }
    
    public class User
    {
        public string ConnectionId { get; init; }
        public string Group { get; init; }
        // public bool Admin { get; set; }
        public bool Moderator { get; set; }
    }
    
    public class Playlist
    {
        public string Group { get; init; }
        public List<string> Videos { get; } = new();
    }

    public static class UsersList
    {
        public static List<User> Users { get; } = new();
    }

    public static class PlaylistList
    {
        public static List<Playlist> Playlists { get; } = new();
    }
}