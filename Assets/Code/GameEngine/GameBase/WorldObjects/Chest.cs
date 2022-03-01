using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using LiteNetLib.Utils;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GameEngine
{
    public class Chest : ServerWorldObject
    {
        private ChestData _chestData { get => (ChestData)_data; set => _data = value; }

        public Chest(WorldVector position) : base(position)
        {
            _canHit = false;
            _layer = ObjectLayer.Container;
            _type = ObjectType.Chest;
            _data = new ChestData();
        }

        public void AddItem(PlayerBagItem item, int count = 1)
        {
            _chestData.AddItem(item, count);
        }

        public PlayerBagItem RemoveItem(int index)
        {
            if (index >= _chestData.Items.Count || index < 0)
                return PlayerBagItem.Lint;
            var item =_chestData.Items[index];
            item.count--;
            if(item.count==0)
                _chestData.Items.RemoveAt(index);
            _update = true;
            if (_chestData.Items.Count==0)
                Unlock(_lockPlayer);
            return item.type;
        }

        public PlayerBagSlot GetItem(int index)
        {
            if (index >= _chestData.Items.Count || index < 0)
                return null;
            return _chestData.Items[index];
        }

        public override bool Lock(ServerPlayer serverPlayer)
        {
            // don't lock if chest is empty
            if (_chestData.Items.Count == 0)
                return false;

            if(base.Lock(serverPlayer))
            {
                _chestData.isOpen = true;
                _update = true;
                return true;
            }
            return false;
        }

        public override bool Unlock(ServerPlayer serverPlayer)
        {
            if(base.Unlock(serverPlayer))
            {
                _chestData.isOpen = false;
                _update = true;
                return true;
            }
            return false;
        }

        public override bool OnHit()
        {
            return false;
        }

    }

    public class ChestData : INetSerializable
    {
        public List<PlayerBagSlot> Items;
        public bool isOpen = false;

        public ChestData()
        {
            Items = new List<PlayerBagSlot>();
        }

        public void AddItem(PlayerBagItem type, int count=1)
        {
            Items.Add(new PlayerBagSlot { type = type, count = count });
        }

        public bool RemoveItem(PlayerBagItem type)
        {
            int index = Items.FindIndex(s => s.type == type);
            if (index != -1)
            {
                Items[index].count--;
                if(Items[index].count==0)
                    Items.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(isOpen);
            writer.Put(Items.Count);
            foreach (var item in Items)
            {
                writer.Put((int)item.type);
                writer.Put(item.count);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            isOpen=reader.GetBool();
            Items.Clear();

            int itemCount = reader.GetInt();

            while(itemCount>0)
            {
                var item = new PlayerBagSlot();
                item.type = (PlayerBagItem)reader.GetInt();
                item.count = reader.GetInt();
                itemCount--;
                Items.Add(item);
            }
        }
    }
}

