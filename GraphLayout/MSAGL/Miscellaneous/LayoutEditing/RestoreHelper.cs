/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using Microsoft.Msagl.Core.Layout;

namespace Microsoft.Msagl.Prototype.LayoutEditing
{
    /// <summary>
    /// Helper class for creating RestoreData objects from GeometryGraph objects.
    /// </summary>
    public static class RestoreHelper
    {
        /// <summary>
        /// creates node restore datat
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(Node node)
        {
            return new NodeRestoreData(node.BoundaryCurve.Clone());
        }

        /// <summary>
        /// gets restore data for an edge
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(Edge edge)
        {
            return new EdgeRestoreData(edge);
        }

        /// <summary>
        /// creates graph restore data
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(GeometryGraph graph)
        {
            return new GraphRestoreData();
        }

        /// <summary>
        /// creates label restore data
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(Label label)
        {
            return new LabelRestoreData(label.Center);
        }

        /// <summary>
        /// calculates the restore data
        /// </summary>
        /// <returns></returns>
        public static RestoreData GetRestoreData(GeometryObject geometryObject)
        {
            Node node = geometryObject as Node;
            if (node != null)
            {
                return GetRestoreData(node);
            }

            Edge edge = geometryObject as Edge;
            if (edge != null)
            {
                return GetRestoreData(edge);
            }

            Label label = geometryObject as Label;
            if (label != null)
            {
                return GetRestoreData(label);
            }

            GeometryGraph graph = geometryObject as GeometryGraph;
            if (graph != null)
            {
                return GetRestoreData(graph);
            }

            return null;
        }
    }
}
