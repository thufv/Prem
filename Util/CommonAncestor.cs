using System.Collections.Generic;
using System.Linq;
using System;

namespace Prem.Util
{
    public static class CommonAncestor
    {
        public static CST.Tree CommonAncestorOf(CST.Tree t1, CST.Tree t2)
        {
            while (true)
            {
                if (t1.depth > t2.depth)
                {
                    t1 = t1.parent;
                }
                else if (t1.depth < t2.depth)
                {
                    t2 = t2.parent;
                }
                else if (t1.id == t2.id)
                {
                    return t1;
                }
                else
                {
                    t1 = t1.parent;
                    t2 = t2.parent;
                }
            }
        }

        public static List<CST.Tree> CommonAncestors(CST.Tree head, List<CST.Tree> tail)
        {
            var pivot = head;
            foreach (var node in tail)
            {
                pivot = CommonAncestorOf(pivot, node);
            }

            return pivot.GetAncestors();
        }
    }
}