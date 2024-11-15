using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.Barracuda;
using System.Collections.Generic;
using System;

namespace Agentics.Inference
{
    public class WorldModelInference : MonoBehaviour 
    {
        [Header("Model Configuration")]
        [SerializeField] private NNModel encoderModel;
        [SerializeField] private NNModel rnnModel;
        [SerializeField] private NNModel controllerModel;
        [SerializeField] private bool useInference = true;
        
        [Header("Inference Settings")]
        [SerializeField] private int latentDimension = 32;
        [SerializeField] private int hiddenDimension = 256;
        [SerializeField] private float predictionHorizon = 1f;
        
        private AgentBrain agentBrain;
        private AgentSensor sensor;
        private MotivationSystem motivation;
        private ConsciousnessSystem consciousness;
        
        // Model execution components
        private ModelExecutor encoder;
        private ModelExecutor rnn;
        private ModelExecutor controller;
        
        // State tracking
        private float[] currentLatentState;
        private float[] currentHiddenState;
        private Queue<float[]> observationHistory;
        private const int MAX_HISTORY = 10;

        private void Awake()
        {
            agentBrain = GetComponent<AgentBrain>();
            sensor = GetComponent<AgentSensor>();
            motivation = GetComponent<MotivationSystem>();
            consciousness = GetComponent<ConsciousnessSystem>();
            
            observationHistory = new Queue<float[]>();
            
            if (useInference)
            {
                InitializeModels();
            }
        }

        private void InitializeModels()
        {
            encoder = new ModelExecutor(encoderModel, "encoder");
            rnn = new ModelExecutor(rnnModel, "rnn");
            controller = new ModelExecutor(controllerModel, "controller");
            
            // Initialize state vectors
            currentLatentState = new float[latentDimension];
            currentHiddenState = new float[hiddenDimension];
        }

        public void UpdateWorldModel()
        {
            if (!useInference) return;

            // Get current observation and context
            var observation = GetCurrentObservation();
            var context = GetContextVector();
            
            // Update world model
            var latentState = encoder.Execute(new Dictionary<string, float[]> {
                { "observation", observation },
                { "context", context }
            })["latent_state"];
            
            // Update RNN state
            var rnnInputs = new Dictionary<string, float[]> {
                { "latent_state", latentState },
                { "hidden_state", currentHiddenState },
                { "context", context }
            };
            
            var rnnOutputs = rnn.Execute(rnnInputs);
            currentHiddenState = rnnOutputs["hidden_state"];
            
            // Generate action
            var controllerInputs = new Dictionary<string, float[]> {
                { "latent_state", latentState },
                { "hidden_state", currentHiddenState },
                { "context", context }
            };
            
            var action = controller.Execute(controllerInputs)["action"];
            
            // Update state tracking
            currentLatentState = latentState;
            UpdateObservationHistory(observation);
            
            // Request action execution
            RequestAction(action);
        }

        private float[] GetCurrentObservation()
        {
            var sensorData = sensor.GetObservationData();
            var motivationData = motivation.GetMotivationalContext();
            var consciousnessData = consciousness.GetConsciousnessState();
            
            return CombineObservations(sensorData, motivationData, consciousnessData);
        }

        private float[] GetContextVector()
        {
            // Combine relevant context information
            var context = new List<float>();
            
            // Add time context
            context.Add(Mathf.Sin(Time.time * Mathf.PI * 2f)); // Day/night cycle
            context.Add(Time.deltaTime);
            
            // Add agent state context
            context.AddRange(motivation.GetMotivationalContext());
            context.AddRange(consciousness.GetConsciousnessState());
            
            return context.ToArray();
        }

        private void UpdateObservationHistory(float[] observation)
        {
            observationHistory.Enqueue(observation);
            if (observationHistory.Count > MAX_HISTORY)
            {
                observationHistory.Dequeue();
            }
        }

        private void RequestAction(float[] action)
        {
            // Convert continuous actions to discrete if needed
            var discreteActions = ConvertToDiscreteActions(action);
            agentBrain.RequestAction(discreteActions);
        }

        private int[] ConvertToDiscreteActions(float[] continuousActions)
        {
            // Convert based on action space configuration
            var discreteActions = new int[continuousActions.Length];
            for (int i = 0; i < continuousActions.Length; i++)
            {
                discreteActions[i] = Mathf.RoundToInt(continuousActions[i]);
            }
            return discreteActions;
        }

        private float[] CombineObservations(params float[][] observations)
        {
            int totalLength = 0;
            foreach (var obs in observations)
            {
                totalLength += obs.Length;
            }
            
            float[] combined = new float[totalLength];
            int currentIndex = 0;
            
            foreach (var obs in observations)
            {
                Array.Copy(obs, 0, combined, currentIndex, obs.Length);
                currentIndex += obs.Length;
            }
            
            return combined;
        }
    }

    public class ModelExecutor
    {
        private IWorker worker;
        private string modelType;
        private Dictionary<string, Tensor> inputs;
        private Dictionary<string, Tensor> outputs;

        public ModelExecutor(NNModel model, string type)
        {
            this.modelType = type;
            var runtimeModel = ModelLoader.Load(model);
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
            inputs = new Dictionary<string, Tensor>();
            outputs = new Dictionary<string, Tensor>();
        }

        public Dictionary<string, float[]> Execute(Dictionary<string, float[]> inputData)
        {
            // Prepare inputs
            foreach (var input in inputData)
            {
                if (inputs.ContainsKey(input.Key))
                {
                    inputs[input.Key].Dispose();
                }
                inputs[input.Key] = new Tensor(input.Value);
            }

            // Execute model
            worker.Execute(inputs);

            // Get outputs
            var results = new Dictionary<string, float[]>();
            foreach (var output in GetOutputNames())
            {
                var tensor = worker.PeekOutput(output);
                results[output] = tensor.ToReadOnlyArray();
                
                if (outputs.ContainsKey(output))
                {
                    outputs[output].Dispose();
                }
                outputs[output] = tensor;
            }

            return results;
        }

        private string[] GetOutputNames()
        {
            switch (modelType)
            {
                case "encoder":
                    return new[] { "latent_state" };
                case "rnn":
                    return new[] { "hidden_state", "predicted_latent" };
                case "controller":
                    return new[] { "action" };
                default:
                    return Array.Empty<string>();
            }
        }

        public void Dispose()
        {
            foreach (var input in inputs.Values)
            {
                input.Dispose();
            }
            foreach (var output in outputs.Values)
            {
                output.Dispose();
            }
            worker.Dispose();
        }
    }
}