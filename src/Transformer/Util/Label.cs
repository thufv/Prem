using System.Xml.Linq;
using System;

namespace Prem.Util
{
    public class Label : ASTSerialization.IObjSerializable
    {
        public int id { get; }

        public string name { get; }

        public Label(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override string ToString() => $"{id}:{name}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Label)obj;
            return this.id == that.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public Type getSerializedType()
        {
            return typeof(Label);
        }

        public XElement serialize()
        {
            var xe = new XElement("Label");
            xe.SetAttributeValue("id",id.ToString());
            xe.SetAttributeValue("name",name);
            return xe;
        }

        public Label(XElement xe)
        {
            id = Int32.Parse(xe.Attribute("id").Value);
            name = xe.Attribute("name").Value;
        }
    }
}