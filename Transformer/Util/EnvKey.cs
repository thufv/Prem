using System.Collections.Generic;
using System.Linq;
using System;
using System.Xml.Linq;
namespace Prem.Util
{
    public class EnvKey : ASTSerialization.IObjSerializable
    {
        private List<int> keys = new List<int>();

        public EnvKey(int id)
        {
            this.keys.Add(id);
        }

        public EnvKey(List<int> keys)
        {
            keys.ForEach(this.keys.Add);
        }

        public EnvKey Append(params int[] ids)
        {
            var key = new EnvKey(keys);
            foreach (var id in ids)
            {
                key.keys.Add(id);
            }
            return key;
        }

        public override string ToString() => string.Join(":", keys.Select(k => k.ToString()));

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (EnvKey)obj;
            return ToString() == that.ToString();
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
        public Type getSerializedType() => typeof(EnvKey);
        public XElement serialize()
        {
            var xe = new XElement("Envkey");
            var keys_xe = new XElement("Attr-keys");
            ASTSerialization.Serialization.fillXElement(keys,keys_xe);
            xe.Add(keys_xe);
            return xe;
        }
        public EnvKey(XElement xe)
        {
            keys = ASTSerialization.Serialization.makeObject(xe.Element("Attr-keys")) as List<int>;
        }
    }
}