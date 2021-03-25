using System.Collections.Generic;
using System.Linq;
using System;
using System.Xml.Linq;
using Microsoft.ProgramSynthesis.Utils;

namespace Prem.Util
{
    public abstract class Feature : ASTSerialization.IObjSerializable
    {
        public static IEnumerable<Feature> Collect(SyntaxNode node) =>
            SubKindOf.Collect(node)
                .Concat(SuperKindOf.Collect(node))
                .Concat(SiblingsContainsFeature.Collect(node))
                .Concat(SiblingsContainsErrToken.Collect(node))
                .Concat(ContainsErrToken.Collect(node));

        public abstract Type getSerializedType();
        public abstract XElement serialize();
    }

    /// <summary>
    /// Feature `SubKindOf(super)`: node `n` is a subkind of `super`, or equivalently,
    /// `super` is a superkind of `n`.
    /// </summary>
    public class SubKindOf : Feature
    {
        public Label super { get; }

        public SubKindOf(Label super)
        {
            this.super = super;
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node) =>
            node.Ancestors().Take(3).Select(n => new SubKindOf(n.label));

        public override string ToString() => $"<: {super}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SubKindOf)obj;
            return that.super.Equals(super);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override Type getSerializedType() => typeof(SubKindOf);
        public override XElement serialize()
        {
            var xe = new XElement("SubKindOf");
            var super_xe = new XElement("Attr-super");
            ASTSerialization.Serialization.fillXElement(super,super_xe);
            xe.Add(super_xe);
            return xe;
        }
        public SubKindOf(XElement xe)
        {
            super = ASTSerialization.Serialization.makeObject(xe.Element("Attr-super")) as Label;
        }
    }

    public class SuperKindOf : Feature
    {
        public Label sub { get; }

        public SuperKindOf(Label sub)
        {
            this.sub = sub;
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            while (node.GetNumChildren() == 1)
            {
                node = node.GetChildren().First();
                yield return new SuperKindOf(node.label);
            }
        }

        public override string ToString() => $">: {sub}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SuperKindOf)obj;
            return that.sub.Equals(sub);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override Type getSerializedType() => typeof(SuperKindOf);
        public override XElement serialize()
        {
            var xe = new XElement("SuperKindOf");
            var sub_xe = new XElement("Attr-sub");
            ASTSerialization.Serialization.fillXElement(sub,sub_xe);
            xe.Add(sub_xe);
            return xe;
        }
        public SuperKindOf(XElement xe)
        {
            sub = ASTSerialization.Serialization.makeObject(xe.Element("Attr-sub")) as Label;
        }
    }


    /// <summary>
    /// Feature `SiblingsContainsLeaf(label, token)`: in the feature scope of node `n`,
    /// there exists a leaf node with label `label` and token `token`.
    /// </summary>
    public class SiblingsContainsLeaf : Feature
    {
        public Label label { get; }

        public string token { get; }

        public SiblingsContainsLeaf(Label label, string token)
        {
            this.label = label;
            this.token = token;
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    yield return new SiblingsContainsLeaf(l.label, l.code);
                }
            }
        }

        public override string ToString() => $"Leaf({label}, \"{token}\")";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsLeaf)obj;
            return that.label.Equals(label) && that.token == token;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(label.GetHashCode(), token.GetHashCode());
        }

        public override Type getSerializedType() => typeof(SiblingsContainsLeaf);
        public override XElement serialize()
        {
            var xe = new XElement("SiblingsContainsLeaf");
            var label_xe = new XElement("Attr-label");
            var token_xe = new XElement("Attr-token");
            ASTSerialization.Serialization.fillXElement(label,label_xe);
            ASTSerialization.Serialization.fillXElement(token,token_xe);
            xe.Add(label_xe);
            xe.Add(token_xe);
            return xe;
        }
        public SiblingsContainsLeaf(XElement xe)
        {
            label = ASTSerialization.Serialization.makeObject(xe.Element("Attr-label")) as Label;
            token = ASTSerialization.Serialization.makeObject(xe.Element("Attr-token")) as string;
        }
    }

    /// <summary>
    /// Feature `SiblingsContainsInfo(label)`: in the feature scope of node `n`,
    /// there exists a leaf node with label `label` and token `t`,
    /// where `t` has the same token as the leaf node with label `label`,
    /// which is inside the error node's `index`-th child.
    /// </summary>
    public class SiblingsContainsFeature : Feature
    {
        public Label label { get; }

        public int index { get; }

        public SiblingsContainsFeature(Label label, int index)
        {
            this.label = label;
            this.index = index;
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    foreach (var index in node.context.LocateErrFeatures(l.label, l.code))
                    {
                        yield return new SiblingsContainsFeature(l.label, index);
                    }
                }
            }
        }

        public override string ToString() => $"~@err[{index}]({label})";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsFeature)obj;
            return that.label.Equals(label) && that.index.Equals(index);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override Type getSerializedType() => typeof(SiblingsContainsFeature);
        public override XElement serialize()
        {
            var xe = new XElement("SiblingsContainsFeature");
            var label_xe = new XElement("Attr-label");
            var index_xe = new XElement("Attr-index");
            ASTSerialization.Serialization.fillXElement(label,label_xe);
            ASTSerialization.Serialization.fillXElement(index,index_xe);
            xe.Add(label_xe);
            xe.Add(index_xe);
            return xe;
        }
        public SiblingsContainsFeature(XElement xe)
        {
            label = ASTSerialization.Serialization.makeObject(xe.Element("Attr-label")) as Label;
            index = (int) ASTSerialization.Serialization.makeObject(xe.Element("Attr-index"));
        }
    }

    /// <summary>
    /// Feature `SiblingsContainsInfo()`: in the feature scope of node `n`,
    /// there exists a leaf node which has the same label and token as the error node.
    /// </summary>
    public class SiblingsContainsErrToken : Feature
    {
        public SiblingsContainsErrToken()
        {
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            var errNode = node.context.err;
            foreach (var p in node.FeatureChildren())
            {
                foreach (var l in p.child.Leaves())
                {
                    if (l.label.Equals(errNode.label) && l.code.Equals(errNode.code))
                    {
                        yield return new SiblingsContainsErrToken();
                    }
                }
            }
        }

        public override string ToString() => $"~@err";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (SiblingsContainsErrToken)obj;
            return true;
        }

        public override int GetHashCode()
        {
            return "~@err".GetHashCode();
        }

        public override Type getSerializedType() => typeof(SiblingsContainsErrToken);
        public override XElement serialize()
        {
            var xe = new XElement("SiblingsContainsErrToken");
            return xe;
        }
        public SiblingsContainsErrToken(XElement xe)
        {
        }
    }

    /// <summary>
    /// Feature `ContainsErrToken()`: in the scope rooted at node `n`,
    /// there exists a leaf node which has the same label and token as the error node.
    /// </summary>
    public class ContainsErrToken : Feature
    {
        public ContainsErrToken()
        {
        }

        public new static IEnumerable<Feature> Collect(SyntaxNode node)
        {
            var errNode = node.context.err;
            if (node.Leaves().Any(l => l.label.Equals(errNode.label) && l.code == errNode.code))
            {
                yield return new ContainsErrToken();
            }
        }

        public override string ToString() => $"@err";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (ContainsErrToken)obj;
            return true;
        }

        public override int GetHashCode()
        {
            return "@err".GetHashCode();
        }

        public override Type getSerializedType() => typeof(ContainsErrToken);
        public override XElement serialize()
        {
            var xe = new XElement("ContainsErrToken");
            return xe;
        }
        public ContainsErrToken(XElement xe)
        {
        }
    }
}