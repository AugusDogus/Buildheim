using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanBuild.Client
{
    internal sealed class ChestObservation : IDisposable
    {
        private readonly MaterialChecklist checklist;
        private readonly Dictionary<string, Subscription> subscriptions = new Dictionary<string, Subscription>();

        private sealed class Subscription : IDisposable
        {
            public Container Container { get; }
            private readonly Inventory inventory;
            private readonly Action refresh;
            public Subscription(Container container, Action refresh)
            {
                Container = container;
                inventory = container.GetInventory();
                this.refresh = refresh;
                inventory.m_onChanged += refresh;
                refresh();
            }
            public void Dispose() => inventory.m_onChanged -= refresh;
        }

        public ChestObservation(MaterialChecklist checklist) { this.checklist = checklist; }

        public static IEnumerable<KeyValuePair<string, int>> Items(Inventory inventory) => inventory.GetAllItems()
            .Select(item => new KeyValuePair<string, int>(item.m_shared.m_name, item.m_stack));

        public void Update()
        {
            foreach (var entry in subscriptions.Where(entry => !entry.Value.Container).ToArray())
            {
                entry.Value.Dispose();
                subscriptions.Remove(entry.Key);
            }
            // Only a chest the player actually opened is observed. No radius scans or access bypasses.
            var container = InventoryGui.instance ? InventoryGui.instance.m_currentContainer : null;
            if (!container || !InventoryGui.IsVisible() || !container.IsOwner()) return;
            var view = container.GetComponent<ZNetView>();
            if (!view || !view.IsValid()) return;
            string key = view.GetZDO().m_uid.ToString();
            if (subscriptions.ContainsKey(key)) return;
            subscriptions.Add(key, new Subscription(container, () => checklist.Observe(key, Items(container.GetInventory()))));
        }

        public void Dispose()
        {
            foreach (var subscription in subscriptions.Values) subscription.Dispose();
            subscriptions.Clear();
            checklist.ForgetChests();
        }
    }
}
