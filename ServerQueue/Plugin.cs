using System;
using System.Collections.Generic;
using System.Linq;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;

namespace ServerQueue
{
    public class Plugin
    {
        [PluginConfig] public Config Config;

        public Dictionary<string, QueueItem> Queue = new();
        private List<string> SlotBypasses = new();
        private List<string> ConfirmedPlayers = new();

        public static Plugin Instance { get; private set; }

        [PluginEntryPoint("ServerQueue", "1.0.0", "Adds a join queue to the server", "ThijsNameIsTaken")]
        private void OnEnabled()
        {
            if (!Config.IsEnabled) return;
            Instance = this;
            EventManager.RegisterEvents(this);
            Log.Info("Plugin has been loaded!", "ServerQueue");
        }

        [PluginEvent]
        public PlayerCheckReservedSlotCancellationData ReservedSlotEvent(PlayerCheckReservedSlotEvent ev)
        {
            if (ev.HasReservedSlot) SlotBypasses.Add(ev.Userid); 
            return PlayerCheckReservedSlotCancellationData.BypassCheck();
        }

        [PluginEvent]
        public PreauthCancellationData PreauthEvent(PlayerPreauthEvent ev)
        {
            if (Server.PlayerCount < Server.MaxPlayers)
            {
                if (Queue.ContainsKey(ev.UserId))
                {
                    if (Queue[ev.UserId].QueuePosition == 0)
                    {
                        Queue.Remove(ev.UserId);
                        foreach (var player in Queue)
                        {
                            player.Value.QueuePosition--;
                        }
                        return PreauthCancellationData.Accept();
                    }
                }
                else
                {
                    return PreauthCancellationData.Accept();
                }
                
            }

            if (SlotBypasses.Contains(ev.UserId))
            {
                SlotBypasses.Remove(ev.UserId);
                return PreauthCancellationData.Accept();
            }
            if (!Queue.ContainsKey(ev.UserId)) 
            {
                Queue.Add(ev.UserId, new QueueItem()
                {
                    UserId = ev.UserId,
                    QueuePosition = Queue.Count,
                    LastUpdate = DateTime.Now
                });
            }
            else
            {
                Queue[ev.UserId].CheckTimers(ev.UserId);
            }
            // Player.Get(ev.UserId).SendConsoleMessage($"Your are in the queue, your queue position is: {Queue[ev.UserId].QueuePosition + 1}");
            if (!ConfirmedPlayers.Contains(ev.UserId))
            {
                ConfirmedPlayers.Add(ev.UserId);
                return PreauthCancellationData.Reject($"Rejoin to enter the queue system. You will automatically join the system once you are at the top of the queue.\nCurrent position in queue: {Queue[ev.UserId].QueuePosition + 1}", true);
            }
            return PreauthCancellationData.RejectDelay((byte)Config.TimeBetweenJoins, true);
        }

       
        
    }

    public class QueueItem
    {
        public string UserId { get; set; }
        public int QueuePosition { get; set; }
        public DateTime LastUpdate { get; set; }

        private CoroutineHandle _timer;

        public QueueItem()
        {
            _timer = Timing.RunCoroutine(QueueTimer(UserId));
        }
        
        internal IEnumerator<float> QueueTimer(string player)
        {
            yield return Timing.WaitForSeconds(Plugin.Instance.Config.TimeBetweenJoins + Plugin.Instance.Config.ErrorMargin);
            Plugin.Instance.Queue.Remove(player);
        }

        public void CheckTimers(string userId)
        {
            if (_timer.IsRunning)
            {
                Timing.KillCoroutines(_timer);
                _timer = Timing.RunCoroutine(QueueTimer(userId));
            }
        }

    }
}