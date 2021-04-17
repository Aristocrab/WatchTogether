// Youtube <iframe>
const tag = document.createElement('script');

tag.src = "https://www.youtube.com/iframe_api";
const firstScriptTag = document.getElementsByTagName('script')[0];
firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

let player;

function onYouTubeIframeAPIReady() {
    player = new YT.Player('player', {
        disablekb: 1,
        modestbranding: 1,
        controls: 1,
        fs: 1,
        events: {
            'onStateChange': onPlayerStateChange
        }
    });
}

let outer = false;

// Disable play/pause for non-admin users
function onPlayerStateChange(event) {
    if (event.data !== -1 && event.data !== 3) {
        console.log(event.data);
        if (!outer) {
            if (event.data === 2) {
                pauseVideo();
            } else if (event.data === 1) {
                let date = new Date();
                playVideo(player.getCurrentTime(), date.getSeconds());
            } else if (event.data === 0) {
                removeFromPlaylist("");
                next();
            }
        } else {
            outer = false;
        }
    }
}

// SignalR
const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/wtg")
    .build();

hubConnection.on("PlayVideo", function (seconds, current) {
    let date = new Date();
    setTime(seconds + Math.abs(current - date.getSeconds()));
    outer = true;
    player.playVideo();
});

hubConnection.on("PauseVideo", function () {
    outer = true;
    player.pauseVideo();
});

hubConnection.on("ChangeVideo", function (id) {
    $("#placeholder").hide();
    outer = true;
    player.loadVideoById(id);
});

hubConnection.on("AddToPlaylist", function (id) {
    $.getJSON("https://www.googleapis.com/youtube/v3/videos?part=id%2C+snippet&id=" + id + "&key=AIzaSyCkQDvBLuFd9cf7nX7UAXzN-vtrQ3VI6ck",
        function (data) {
            $("#playlist").append('<li onclick="removeFromPlaylist(\'' + id + '\')" id="' + id + '" class=\"list-group-item list-group-item-action\"><img src=\"' + data["items"][0]["snippet"]["thumbnails"]["standard"]["url"] + '\" height=\"54px\" width=\"96px\" alt=\"\">' + data["items"][0]["snippet"]["title"] + ' <small class=\"text-muted\">' + data["items"][0]["snippet"]["channelTitle"] + '</small></li>\n');
        }
    );
});

hubConnection.on("RemoveFromPlaylist", function (id) {
    console.log("removed id:" + id);
    $("#playlist").find('li').each(function () {
        if($(this).attr("id") === id)  {
            $(this).remove();
        }
    })
});

hubConnection.on("SetGroup", function (group) {
    $("#group").text(group);
});

hubConnection.on("AddMember", function (id) {
    $("#usersList").append('<li class="list-group-item list-group-item-action d-flex justify-content-between align-items-start"><div><div>' + id + '<span class="text-body" onclick="kickUser(\'' + id + '\')"> &#215;</span></div></div><div><a title="Make a moderator" href="#"><span onclick="makeModerator(\'' + id + '\')" class="badge bg-primary">M</span></a></div></li>');
})

hubConnection.on("AddAdmin", function (id) {
    $("#usersList").append('<li class="list-group-item list-group-item-action d-flex justify-content-between align-items-start"><div><div>' + id + '<small class="text-muted"> Admin</small></div></div></li>');
});

hubConnection.on("ToggleModerator", function (id, moderator) {
    $("li.d-flex").each(function () {
        if ($(this).find("div").first().find("div").first().text().slice(0, -2) === id || $(this).find("div").first().find("div").first().text().slice(0, -10) === 10) {
            $(this).find("div:eq(2) > a > span").replaceWith('<span onclick="makeModerator(\'' + id + '\')" class="badge bg-primary' + (moderator ? " enabled" : "") + '">M</span>');
        }
    })
});

hubConnection.on("KickUser", function (id) {
    $("li.d-flex").each(function () {
        if ($(this).find("div").first().find("div").first().text().slice(0, -2) === id || $(this).find("div").first().find("div").first().text().slice(0, -10) === 10) {
            $(this).remove();
        }
    })
});

hubConnection.on("Kick", function () {
    window.location.replace("/w");
});

function addToPlaylist(url) {
    const id = url.substring(url.indexOf('=') + 1).substring(0, 11);
    hubConnection.invoke("AddToPlaylist", id);
}

function getVideoData(url) {
    $.getJSON("https://www.googleapis.com/youtube/v3/videos?part=id%2C+snippet&id=cZmH04yRgH0&key=AIzaSyCkQDvBLuFd9cf7nX7UAXzN-vtrQ3VI6ck",
        function (data) {
            $("#th").attr("src", data["items"][0]["snippet"]["thumbnails"]["standard"]["url"]);
        }
    );
}

function playVideo(seconds, current) {
    hubConnection.invoke("PlayVideo", seconds, current);
}

function pauseVideo() {
    hubConnection.invoke("PauseVideo");
}

function setTime(seconds = 10) {
    outer = true;
    player.seekTo(seconds);
}

function joinGroup(group) {
    if (document.getElementById('group').innerHTML === "None") {
        hubConnection.invoke("JoinGroup", group);
    }
}

function next() {
    hubConnection.invoke("NextInPlaylist");
}

function removeFromPlaylist(id) {
    hubConnection.invoke("RemoveFromPlaylist", id);
}

function makeModerator(id) {
    hubConnection.invoke("ToggleModerator", id);
}

function kickUser(id) {
    hubConnection.invoke("KickUser", id);
}

hubConnection.start().then(function () {
    const urlParams = new URLSearchParams(window.location.search);
    joinGroup(urlParams.get('id'));
}); 