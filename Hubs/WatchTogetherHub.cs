using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using static WatchTogether.Hubs.PlaylistList;
using static WatchTogether.Hubs.UsersList;

namespace WatchTogether.Hubs
{
    public class WatchTogether : Hub
    {
        public async Task AddToPlaylist(string videoId)
        {
            if (Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                {
                    try
                    {
                        var group = Users.First(u => u.ConnectionId == Context.ConnectionId).Group;

                        if (!Playlists.First(p => p.Group == group).Videos.Contains(videoId))
                        {
                            Playlists.First(p => p.Group == group).Videos.Add(videoId);
                            await Clients.Group(user.Group).SendAsync("AddToPlaylist", videoId);

                            if (Playlists.First(p => p.Group == group).Videos.Count == 1)
                                await Clients.Group(user.Group).SendAsync("ChangeVideo", videoId);
                        }
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
            if (Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                {
                    var group = Users.First(u => u.ConnectionId == Context.ConnectionId).Group;
                    if (Playlists.First(p => p.Group == group).Videos.Any())
                    {
                        await Clients.Group(user.Group)
                            .SendAsync("ChangeVideo", Playlists.First(p => p.Group == group).Videos[0]);
                    }
                }
            }
        }

        public async Task RemoveFromPlaylist(string id)
        {
            if (Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                {
                    var group = Users.First(u => u.ConnectionId == Context.ConnectionId).Group;

                    if (string.IsNullOrEmpty(id))
                    {
                        await Clients.Group(user.Group).SendAsync("RemoveFromPlaylist",
                            Playlists.First(p => p.Group == group).Videos[0]);
                        Playlists.First(p => p.Group == group).Videos.RemoveAt(0);
                    }
                    else
                    {
                        await Clients.Group(user.Group).SendAsync("RemoveFromPlaylist", id);
                        Playlists.First(p => p.Group == group).Videos.Remove(id);
                    }

                    if (Playlists.First(p => p.Group == group).Videos.Any())
                    {
                        await Clients.Group(user.Group)
                            .SendAsync("ChangeVideo",
                                Playlists.First(p => p.Group == group).Videos[0]);
                    }
                    else
                    {
                        await Clients.Group(user.Group).SendAsync("PlayEnded");
                    }
                }
            }
        }

        public async Task PlayVideo()
        {
            if (Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                    await Clients.GroupExcept(user.Group, Context.ConnectionId).SendAsync("PlayVideo");
                else
                    await Clients.Caller.SendAsync("PauseVideo");
            }
        }

        public async Task SyncTime(double time, double current)
        {
            if (Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                    await Clients.GroupExcept(user.Group, Context.ConnectionId).SendAsync("SyncTime", time, current);
            }
        }

        public async Task PauseVideo()
        {
            if (Users.Any(u => u.ConnectionId == Context.ConnectionId))
            {
                var user = Users.First(u => u.ConnectionId == Context.ConnectionId);
                if (user.Moderator)
                    await Clients.GroupExcept(user.Group, Context.ConnectionId).SendAsync("PauseVideo");
                else
                    await Clients.Caller.SendAsync("PlayVideo");
            }
        }

        public async Task JoinGroup(string group)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            if (Users.All(u => u.Group != group))
            {
                Users.Add(new User
                {
                    ConnectionId = Context.ConnectionId,
                    Group = group,
                    Moderator = true,
                    Admin = true
                });
                if (Playlists.All(p => p.Group != group))
                {
                    Playlists.Add(new Playlist {Group = group});
                }

                await Clients.Caller.SendAsync("SetGroup", $"Admin of {group}");
                await Clients.Group(group).SendAsync("AddAdmin", Context.ConnectionId);
            }
            else
            {
                Users.Add(new User {ConnectionId = Context.ConnectionId, Group = group});
                await Clients.Caller.SendAsync("SetGroup", $"User of {group}");
                await Clients.Group(group).SendAsync("AddMember", Context.ConnectionId);

                // if (Playlists.First(p => p.Group == group).Videos.Any())
                // {
                //     Console.WriteLine(1);
                //     Thread.Sleep(1000);
                //     await Clients.Caller.SendAsync("ChangeVideo", Playlists.First(p => p.Group == group).Videos.First());
                //     Thread.Sleep(1000);
                //     await Clients.Client(Users.First(u => u.Group == group && u.Admin).ConnectionId)
                //         .SendAsync("SyncWithNew", Context.ConnectionId);
                // }
            }
        }

        public async Task ToggleModerator(string connectionId)
        {
            var user = Users.Where(u => u.ConnectionId == connectionId).ToList();
            var caller = Users.First(u => u.ConnectionId == Context.ConnectionId);

            if (user.Any() && caller.Moderator)
            {
                user.First().Moderator = !user.First().Moderator;
                await Clients.Group(user.First().Group)
                    .SendAsync("ToggleModerator", connectionId, user.First().Moderator);
            }
        }

        public async Task KickUser(string connectionId)
        {
            var user = Users.Where(u => u.ConnectionId == connectionId).ToList();
            var caller = Users.First(u => u.ConnectionId == Context.ConnectionId);

            if (user.Any() && caller.Moderator)
            {
                user.First().Moderator = false;
                await Clients.GroupExcept(caller.Group, user.First().ConnectionId).SendAsync("KickUser", connectionId);
                await Clients.Client(user.First().ConnectionId).SendAsync("Kick");
                Users.Remove(user.First());
            }
        }
    }

    public class User
    {
        public string ConnectionId { get; init; }

        public string Group { get; init; }

        public bool Admin { get; init; }
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