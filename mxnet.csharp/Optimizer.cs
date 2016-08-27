﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OptimizerCreator = System.IntPtr;
using OptimizerHandle = System.IntPtr;

namespace mxnet.csharp
{
    public class Optimizer
    {
        bool init_ = false;
        private float learning_rate_;
        float weight_decay_;
        string opt_type_;
        OptimizerHandle handle_;
        OptimizerCreator creator_;
        Dictionary<string, string> params_ = new Dictionary<string, string>();

        public Optimizer(string opt_type, float learning_rate, float weight_decay)

        {
            learning_rate_ = learning_rate;
            weight_decay_ = weight_decay;
            opt_type_ = opt_type;
            NativeMethods.MXOptimizerFindCreator(opt_type, out creator_);
        }

        public void Update(int index, NDArray weight, NDArray grad,
            float learning_rate,
            float weight_decay)
        {
            if (!init_)
            {
                List<string> param_keys = new List<string>();
                List<string> param_values = new List<string>();
                foreach (var k_v in params_)
                {
                    param_keys.Add(k_v.Key);
                    param_values.Add(k_v.Value);
                }
                NativeMethods.MXOptimizerCreateOptimizer(creator_, (uint)params_.Count, param_keys.ToArray(),
                                           param_values.ToArray(), out handle_);
                init_ = true;
            }
            learning_rate_ = learning_rate;
            weight_decay_ = weight_decay;
            NativeMethods.MXOptimizerUpdate(handle_, index, weight.GetHandle(), grad.GetHandle(),
                learning_rate_, weight_decay_);
        }

        public void Update(int index, NDArray weight, NDArray grad)
        {
            Update(index, weight, grad, learning_rate_, weight_decay_);
        }


        /// <summary>
        /// set config parameters
        /// </summary>
        /// <typeparam name="TT"></typeparam>
        /// <param name="name">name of the config parameter</param>
        /// <param name="value">value of the config parameter</param>
        /// <returns></returns>
        public Optimizer SetParam<TT>(string name, TT value)
        {
            if (value == null)
            {
                return this;
            }
            params_[name] = value.ToString();
            return this;
        }

        public string Serialize()
        {
            var @params = params_;
            @params["opt_type"] = opt_type_;
            @params["learning_rate"] = Convert.ToString(learning_rate_);
            @params["weight_decay"] =  Convert.ToString(weight_decay_);
            return string.Join("\n", @params.Select(s => $"{s.Key}=={s.Value}"));
        }

        private static void Update(Optimizer optimizer , int index, NDArray weight, NDArray grad,Dictionary<int, NDArray> states)
        {
            if (!states.ContainsKey(index))
            {
                states[index] = optimizer.create_state(index, weight);
            }

            optimizer.update(index, weight, grad, states[index]);
        }

        public static Action<int, NDArray, NDArray> get_updater(Optimizer optimizer)
        {
            Dictionary<int, NDArray> states = new Dictionary<int, NDArray>();

            return (int index, NDArray weight, NDArray grad) => { Update(optimizer, index, weight, grad, states); };
        }
    }
}