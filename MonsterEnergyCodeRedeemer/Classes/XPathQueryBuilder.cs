﻿using HtmlAgilityPack;
using System.Text;

namespace MonsterEnergyCodeRedeemer.Classes
{
    internal class XPathQueryBuilder
    {
        private List<HtmlNode>? _nodes = [];

        /// <summary>
        ///     Return all resulting nodes from the list
        /// </summary>
        internal List<HtmlNode>? Results => _nodes;

        /// <summary>
        ///     Returns only the first node result from the list
        /// </summary>
        internal HtmlNode? Result
        {
            get
            {
                if (_nodes.Count > 0)
                {
                    return _nodes[0];
                }

                return default;
            }
        }

        internal XPathQueryBuilder Query(HtmlDocument doc)
        {
            _nodes?.Add(doc.DocumentNode);

            return this;
        }

        internal XPathQueryBuilder Query(string html)
        {
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            _nodes?.Add(doc.DocumentNode);

            return this;
        }

        internal XPathQueryBuilder Query(HtmlNode node)
        {
            _nodes.Add(node);

            return this;
        }
        internal XPathQueryBuilder Query(List<HtmlNode> nodes)
        {
            _nodes.AddRange(nodes);

            return this;
        }

        internal XPathQueryBuilder ById(string id)
        {
            string query = $".//*[@id='{id}']";

            _nodes = GetNodesByQuery(query);

            return this;
        }
        internal XPathQueryBuilder ByElement(string elementName)
        {
            string query = ".//" + elementName;

            _nodes = GetNodesByQuery(query);

            return this;
        }
        internal XPathQueryBuilder ByClass(params string[] classNames)
        {
            StringBuilder _builder = new();

            foreach (string className in classNames)
            {
                _builder.Append(className);
                _builder.Append(' ');
            }

            _builder.Remove(_builder.Length - 1, 1);

            string query = $".//*[@class='{_builder}']";

            _nodes = GetNodesByQuery(query);

            return this;
        }
        internal XPathQueryBuilder ByAttribute(params string[] attributeNames)
        {
            StringBuilder _builder = new();

            foreach (string className in attributeNames)
            {
                _builder.Append(className);
                _builder.Append(' ');
            }

            _builder.Remove(_builder.Length - 1, 1);

            string query = $".//*[@{_builder}]";

            _nodes = GetNodesByQuery(query);

            return this;
        }
        internal XPathQueryBuilder ByAttributeValue(string attributeName, string attributeValue)
        {
            string query = $".//*[@{attributeName}='{attributeValue}']";

            _nodes = GetNodesByQuery(query);

            return this;
        }
        internal XPathQueryBuilder ByAttributeValues(string attributeName, List<string> attributeValues)
        {
            List<HtmlNode> filteredNodes = [];
            foreach (string attValue in attributeValues)
            {
                string query = $".//*[@{attributeName}='{attValue}']";

                filteredNodes.AddRange(GetNodesByQuery(query));
            }

            _nodes = filteredNodes;

            return this;
        }
        internal List<HtmlNode> GetNodesByQuery(string query)
        {
            List<HtmlNode> nodes = [];

            for (int i = 0; i < _nodes?.Count; i++)
            {
                List<HtmlNode> newNodes = _nodes[i].SelectNodes(query)?.ToList();

                if (newNodes is null) continue;

                nodes.AddRange(newNodes);
            }

            return nodes;
        }
    }
}
