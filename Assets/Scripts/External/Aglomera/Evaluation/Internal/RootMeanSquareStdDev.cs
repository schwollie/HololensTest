﻿// ------------------------------------------
// <copyright file="RootMeanSquareStdDev.cs" company="Pedro Sequeira">
// 
//     Copyright (c) 2018 Pedro Sequeira
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to the following conditions:
//  
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
// </copyright>
// <summary>
//    Project: Aglomera
//    Last updated: 05/14/2018
//    Author: Pedro Sequeira
//    E-mail: pedrodbs@gmail.com
// </summary>
// ------------------------------------------

using System;
using System.Linq;

namespace Aglomera.Evaluation.Internal
{
    /// <summary>
    ///     Implements an internal evaluation method measuring the root-mean-square standard deviation (RMSSD), i.e., the
    ///     square root of the variance between all elements. This criterion considers only the compactness of the clustering
    ///     partition.
    /// </summary>
    /// <remarks>
    ///     In order to select the optimal partition / <see cref="ClusterSet{TInstance}" /> using this criterion given
    ///     some <see cref="ClusteringResult{TInstance}" /> one has to find the 'knee' in the plot of the criterion value vs.
    ///     the number of clusters.
    /// </remarks>
    /// <typeparam name="TInstance">The type of instance considered.</typeparam>
    public class RootMeanSquareStdDev<TInstance> : IInternalEvaluationCriterion<TInstance>
        where TInstance : IComparable<TInstance>

    {
        #region Fields

        private readonly CentroidFunction<TInstance> _centroidFunc;

        #endregion

        #region Constructors

        /// <summary>
        ///     Creates a new <see cref="RootMeanSquareStdDev{TInstance}" /> with given dissimilarity metric.
        /// </summary>
        /// <param name="dissimilarityMetric">The metric used to calculate dissimilarity between cluster elements.</param>
        /// <param name="centroidFunc">
        ///     A function to get an element representing the centroid of a <see cref="Cluster{TInstance}" />.
        /// </param>
        public RootMeanSquareStdDev(
            IDissimilarityMetric<TInstance> dissimilarityMetric, CentroidFunction<TInstance> centroidFunc)
        {
            this._centroidFunc = centroidFunc;
            this.DissimilarityMetric = dissimilarityMetric;
        }

        #endregion

        #region Properties & Indexers

        /// <inheritdoc />
        public IDissimilarityMetric<TInstance> DissimilarityMetric { get; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public double Evaluate(ClusterSet<TInstance> clusterSet)
        {
            // undefined if only one cluster
            if (clusterSet.Count < 2) return double.NaN;

            // gets clusters' centroids 
            var centroids = clusterSet.Select(t => this._centroidFunc(t)).ToList();

            var n = 0;
            var sum = 0d;
            for (var i = 0; i < clusterSet.Count; i++)
            {
                n += clusterSet[i].Count;

                // updates sum of squared distances to centroids
                foreach (var instance in clusterSet[i])
                {
                    var dist = this.DissimilarityMetric.Calculate(instance, centroids[i]);
                    sum += dist * dist;
                }
            }

            return Math.Sqrt(sum / n);
        }

        #endregion
    }
}