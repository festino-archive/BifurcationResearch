using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Bifurcation
{
    public class DependencySpace
    {
        private List<DependencyNode> AllNodes = new List<DependencyNode>();
        private List<Tuple<string, Complex>> Values = new List<Tuple<string, Complex>>();

        public DependencySpace()
        {

        }

        public void Set(string name, Complex val)
        {
            var newTuple = Tuple.Create(name, val);
            for (int i = Values.Count - 1; i >= 0; i--)
                if (Values[i].Item1 == name)
                {
                    Values[i] = newTuple;
                    return;
                }
            Values.Add(newTuple);
        }

        public Complex? Get(string name)
        {
            foreach (var val in Values)
                if (val.Item1 == name)
                    return val.Item2;
            foreach (DependencyNode node in AllNodes)
                if (name == node.Name)
                    return node.Value;
            return null;
        }

        public void Add(DependencyNode newNode)
        {
            foreach (DependencyNode node in AllNodes)
                if (newNode.Name == node.Name)
                    throw new Exception("parameter duplication : " + newNode.Name);
            AllNodes.Add(newNode);
            newNode.DependenciesChanged += (node, dep) => DependenciesChanged(node, dep);
        }

        public void RemoveFilter()
        {
            string pattern = "P\\(*,*\\)"; // P(K,N)
            for (int i = AllNodes.Count - 1; i >= 0; i--)
            {
                if (Regex.IsMatch(AllNodes[i].Name, pattern))
                    AllNodes.RemoveAt(i);
            }
        }

        private void DependenciesChanged(DependencyNode node, string[] dep)
        {
            foreach (DependencyNode n in node.DependsOn)
                n.Dependencies.Remove(node);
            node.DependsOn.Clear();
            foreach (string name in dep)
                if (name == node.Name)
                    throw new Exception("self dependency : " + name);
            foreach (DependencyNode n in AllNodes)
                foreach (string name in dep)
                    if (n.Name == name)
                    {
                        n.Dependencies.Add(node);
                        node.DependsOn.Add(n);
                        break;
                    }
            // name with no node?
        }
    }
}
