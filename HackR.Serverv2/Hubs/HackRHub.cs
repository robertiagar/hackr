using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace HackR.Server.Hubs
{
    public class User
    {
        public string Name { get; set; }
        public HashSet<string> ConnectionIds { get; set; }
    }

    [Authorize]
    public class HackRHub : Hub
    {
        private static readonly ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>(StringComparer.InvariantCultureIgnoreCase);

        public void Send(string message)
        {
            string sender = Context.User.Identity.Name;

            Clients.All.received(new { sender = sender, message = message, isPrivate = false });
        }

        public void Send(string message, string to)
        {
            User receiver;
            if (Users.TryGetValue(to, out receiver))
            {
                User sender = GetUser(Context.User.Identity.Name);

                IEnumerable<string> allReceivers;
                lock (receiver.ConnectionIds)
                {
                    lock (sender.ConnectionIds)
                    {
                        allReceivers = receiver.ConnectionIds.Concat(sender.ConnectionIds);
                    }
                }

                foreach (var cid in allReceivers)
                {
                    Clients.Client(cid).received(new { sender = sender.Name, message = message, isPrivate = true });
                }
            }
        }

        public IEnumerable<string> GetConnectedUsers()
        {
            return Users.Where(x =>
            {
                lock (x.Value.ConnectionIds)
                {
                    return !x.Value.ConnectionIds.Contains(Context.ConnectionId, StringComparer.InvariantCultureIgnoreCase);
                }
            }).Select(x => x.Key);
        }

        public override Task OnConnected()
        {
            string username = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;

            var user = Users.GetOrAdd(username, _ => new User
            {
                Name = username,
                ConnectionIds = new HashSet<string>()
            });

            lock (user.ConnectionIds)
            {
                user.ConnectionIds.Add(connectionId);
                if (user.ConnectionIds.Count == 1)
                {
                    Clients.Others.userConnected(username);
                }
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string username = Context.User.Identity.Name;
            string connectionId = Context.ConnectionId;
            User user;
            Users.TryGetValue(username, out user);
            
            if (user != null)
            {
                lock (user.ConnectionIds)
                {
                    user.ConnectionIds.RemoveWhere(cid => cid.Equals(connectionId));
                    if (!user.ConnectionIds.Any())
                    {
                        User removedUser;
                        Users.TryRemove(username, out removedUser);

                        Clients.Others.userDisconnected(username);
                    }
                }
            }

            return base.OnDisconnected(stopCalled);
        }

        private User GetUser(string username)
        {
            User user;
            Users.TryGetValue(username, out user);

            return user;
        }
    }
}
