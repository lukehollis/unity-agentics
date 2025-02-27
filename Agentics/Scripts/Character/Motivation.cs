// using UnityEngine;
// using System;
// using Unity.MLAgents;
// using Agentics;

// namespace Agentics
// {
//     [System.Serializable]
//     public class EmotionalState
//     {
//         public float happiness;    // -1 to 1
//         public float energy;       // 0 to 1 
//         public float stress;       // 0 to 1
//         public float socialNeed;   // 0 to 1
//         public float confidence;   // 0 to 1

//         // Emotional decay rates (per second)
//         public float happinessDecay = 0.05f;
//         public float energyDecay = 0.03f;
//         public float stressRecovery = 0.08f;
//         public float socialDecay = 0.07f;
//     }

//     [System.Serializable]
//     public class InnateNeeds
//     {
//         public float rest;         // 0 to 1
//         public float hunger;       // 0 to 1
//         public float comfort;      // 0 to 1
//         public float achievement;  // 0 to 1
        
//         // Need accumulation rates (per second)
//         public float restRate = 0.08f;
//         public float hungerRate = 0.12f;
//         public float comfortRate = 0.04f;
//     }

//     public class MotivationSystem : MonoBehaviour
//     {
//         [SerializeField] private EmotionalState emotions = new EmotionalState();
//         [SerializeField] private InnateNeeds needs = new InnateNeeds();
        
//         [Header("Personality Traits")]
//         [SerializeField, Range(0,1)] private float extraversion = 0.5f;
//         [SerializeField, Range(0,1)] private float neuroticism = 0.5f;
//         [SerializeField, Range(0,1)] private float conscientiousness = 0.5f;

//         [Header("Motivation Settings")]
//         [SerializeField] private float motivationUpdateInterval = 0.5f;
//         [SerializeField] private float emotionalInfluenceStrength = 0.3f;

//         private Brain agentBrain;
//         private float lastMotivationUpdate;

//         private void Awake()
//         {
//             agentBrain = GetComponent<Brain>();
//         }

//         private void Update()
//         {
//             if (Time.time - lastMotivationUpdate >= motivationUpdateInterval)
//             {
//                 UpdateMotivationalState();
//                 lastMotivationUpdate = Time.time;
//             }
//         }

//         private void UpdateMotivationalState()
//         {
//             UpdateNeeds();
//             UpdateEmotions();
//             ApplyMotivationalRewards();
//         }

//         private void UpdateNeeds()
//         {
//             // Update basic needs over time
//             needs.rest = Mathf.Clamp01(needs.rest - needs.restRate * motivationUpdateInterval);
//             needs.hunger = Mathf.Clamp01(needs.hunger + needs.hungerRate * motivationUpdateInterval);
//             needs.comfort = Mathf.Clamp01(needs.comfort - needs.comfortRate * motivationUpdateInterval);

//             // Achievement decays slowly over time
//             needs.achievement = Mathf.Clamp01(needs.achievement - 0.02f * motivationUpdateInterval);
//         }

//         private void UpdateEmotions()
//         {
//             // Calculate base emotional changes
//             UpdateHappiness();
//             UpdateEnergy();
//             UpdateStress();
//             UpdateSocialNeed();
//             UpdateConfidence();

//             // Apply personality influences
//             ApplyPersonalityEffects();
//         }

//         private void UpdateHappiness()
//         {
//             float needsSatisfaction = (needs.rest + (1 - needs.hunger) + needs.comfort) / 3f;
//             float targetHappiness = Mathf.Lerp(-1f, 1f, needsSatisfaction);
//             emotions.happiness = Mathf.Lerp(emotions.happiness, targetHappiness, 
//                 emotions.happinessDecay * motivationUpdateInterval);
//         }

//         private void UpdateEnergy()
//         {
//             float targetEnergy = needs.rest * (1 - needs.hunger * 0.5f);
//             emotions.energy = Mathf.Lerp(emotions.energy, targetEnergy, 
//                 emotions.energyDecay * motivationUpdateInterval);
//         }

//         private void UpdateStress()
//         {
//             float unfullfilledNeeds = (needs.hunger + (1 - needs.rest) + (1 - needs.comfort)) / 3f;
//             float targetStress = Mathf.Lerp(0f, 1f, unfullfilledNeeds);
//             emotions.stress = Mathf.Lerp(emotions.stress, targetStress, 
//                 emotions.stressRecovery * motivationUpdateInterval);
//         }

//         private void UpdateSocialNeed()
//         {
//             float targetSocial = Mathf.Lerp(0.3f, 0.8f, extraversion);
//             emotions.socialNeed = Mathf.Lerp(emotions.socialNeed, targetSocial, 
//                 emotions.socialDecay * motivationUpdateInterval);
//         }

//         private void UpdateConfidence()
//         {
//             float targetConfidence = Mathf.Lerp(0.3f, 1f, needs.achievement);
//             targetConfidence *= (1 - emotions.stress * 0.5f);
//             emotions.confidence = Mathf.Lerp(emotions.confidence, targetConfidence, 
//                 0.1f * motivationUpdateInterval);
//         }

//         private void ApplyPersonalityEffects()
//         {
//             // Extraversion influences social need and happiness baseline
//             emotions.socialNeed += (extraversion - 0.5f) * 0.2f;
//             emotions.happiness += (extraversion - 0.5f) * 0.1f;

//             // Neuroticism influences stress and confidence
//             emotions.stress += neuroticism * 0.2f;
//             emotions.confidence -= neuroticism * 0.2f;

//             // Conscientiousness influences energy and achievement
//             emotions.energy += (conscientiousness - 0.5f) * 0.2f;
//             needs.achievement += (conscientiousness - 0.5f) * 0.1f;

//             ClampEmotionalValues();
//         }

//         private void ApplyMotivationalRewards()
//         {
//             if (agentBrain == null) return;

//             // Convert emotional state to rewards/penalties
//             float emotionalReward = 0f;

//             // Positive emotions contribute to positive rewards
//             emotionalReward += emotions.happiness * 0.3f;
//             emotionalReward += emotions.confidence * 0.2f;
//             emotionalReward += emotions.energy * 0.2f;

//             // Negative states contribute to penalties
//             emotionalReward -= emotions.stress * 0.4f;
//             emotionalReward -= emotions.socialNeed * 0.2f;

//             // Apply the emotional reward
//             agentBrain.AddReward(emotionalReward * emotionalInfluenceStrength * motivationUpdateInterval);
//         }

//         public float[] GetMotivationalContext()
//         {
//             return new float[]
//             {
//                 emotions.happiness,
//                 emotions.energy,
//                 emotions.stress,
//                 emotions.socialNeed,
//                 emotions.confidence,
//                 needs.rest,
//                 needs.hunger,
//                 needs.comfort,
//                 needs.achievement,
//                 extraversion,
//                 neuroticism,
//                 conscientiousness
//             };
//         }

//         private void ClampEmotionalValues()
//         {
//             emotions.happiness = Mathf.Clamp(emotions.happiness, -1f, 1f);
//             emotions.energy = Mathf.Clamp01(emotions.energy);
//             emotions.stress = Mathf.Clamp01(emotions.stress);
//             emotions.socialNeed = Mathf.Clamp01(emotions.socialNeed);
//             emotions.confidence = Mathf.Clamp01(emotions.confidence);
//         }

//         // Public methods for external systems to influence motivation
//         public void ModifyEmotions(EmotionalState modification)
//         {
//             emotions.happiness += modification.happiness;
//             emotions.energy += modification.energy;
//             emotions.stress += modification.stress;
//             emotions.socialNeed += modification.socialNeed;
//             emotions.confidence += modification.confidence;
//             ClampEmotionalValues();
//         }

//         public void SatisfyNeed(string needType, float amount)
//         {
//             switch (needType.ToLower())
//             {
//                 case "rest":
//                     needs.rest = Mathf.Clamp01(needs.rest + amount);
//                     break;
//                 case "hunger":
//                     needs.hunger = Mathf.Clamp01(needs.hunger - amount);
//                     break;
//                 case "comfort":
//                     needs.comfort = Mathf.Clamp01(needs.comfort + amount);
//                     break;
//                 case "achievement":
//                     needs.achievement = Mathf.Clamp01(needs.achievement + amount);
//                     break;
//             }
//         }

//         public void ProcessPlanContext(string planOverview)
//         {
//             // Parse emotional context from the plan overview
//             bool containsPositive = planOverview.ToLower().Contains("happy") || 
//                                   planOverview.ToLower().Contains("excited") ||
//                                   planOverview.ToLower().Contains("good");
                          
//             bool containsNegative = planOverview.ToLower().Contains("sad") ||
//                                    planOverview.ToLower().Contains("tired") ||
//                                    planOverview.ToLower().Contains("stressed");
            
//             // Create emotional modification based on plan context
//             EmotionalState modification = new EmotionalState();
            
//             if (containsPositive)
//             {
//                 modification.happiness = 0.2f;
//                 modification.energy = 0.1f;
//                 modification.confidence = 0.1f;
//             }
            
//             if (containsNegative)
//             {
//                 modification.stress = 0.2f;
//                 modification.energy = -0.1f;
//                 modification.confidence = -0.1f;
//             }
            
//             // Apply the emotional modifications
//             ModifyEmotions(modification);
//         }

//         public float GetEmotionalWeightForAction(string actionType)
//         {
//             float weight = 0f;
            
//             switch (actionType.ToLower())
//             {
//                 case "rest":
//                     weight += (1 - emotions.energy) * 0.4f;
//                     weight += emotions.stress * 0.3f;
//                     break;
                    
//                 case "eat":
//                     weight += needs.hunger * 0.5f;
//                     weight -= emotions.stress * 0.2f;
//                     break;
                    
//                 case "socialize":
//                     weight += emotions.socialNeed * 0.4f;
//                     weight += emotions.happiness * 0.2f;
//                     weight -= emotions.stress * 0.2f;
//                     break;
                    
//                 case "work":
//                     weight += needs.achievement * 0.3f;
//                     weight += emotions.confidence * 0.2f;
//                     weight += emotions.energy * 0.2f;
//                     break;
                    
//                 case "explore":
//                     weight += emotions.energy * 0.3f;
//                     weight += (1 - emotions.stress) * 0.2f;
//                     weight += emotions.confidence * 0.2f;
//                     break;
//             }
            
//             // Personality influences
//             weight += (extraversion - 0.5f) * 0.1f; // More extraverted agents are slightly more motivated
//             weight -= neuroticism * 0.2f; // More neurotic agents are less motivated
//             weight += conscientiousness * 0.2f; // More conscientious agents are more motivated
            
//             return Mathf.Clamp01(weight);
//         }

//         public float GetNeedWeightForAction(string actionType)
//         {
//             float weight = 0f;
            
//             switch (actionType.ToLower())
//             {
//                 case "rest":
//                     weight += (1 - needs.rest) * 0.6f;
//                     break;
                    
//                 case "eat":
//                     weight += needs.hunger * 0.7f;
//                     break;
                    
//                 case "comfort":
//                     weight += (1 - needs.comfort) * 0.4f;
//                     break;
                    
//                 case "work":
//                 case "achieve":
//                     weight += (1 - needs.achievement) * 0.5f;
//                     break;
//             }
            
//             return Mathf.Clamp01(weight);
//         }

//         public float GetActionCompletionBonus(string actionType)
//         {
//             float bonus = 0f;
            
//             switch (actionType.ToLower())
//             {
//                 case "rest":
//                     bonus += (1 - emotions.energy) * 0.4f;
//                     bonus += emotions.stress * 0.3f;
//                     break;
                    
//                 case "eat":
//                     bonus += needs.hunger * 0.5f;
//                     bonus -= emotions.stress * 0.2f;
//                     break;
                    
//                 case "socialize":
//                     bonus += emotions.socialNeed * 0.4f;
//                     bonus += emotions.happiness * 0.2f;
//                     bonus -= emotions.stress * 0.2f;
//                     break;
                    
//                 case "work":
//                     bonus += needs.achievement * 0.3f;
//                     bonus += emotions.confidence * 0.2f;
//                     bonus += emotions.energy * 0.2f;
//                     break;
                    
//                 case "explore":
//                     bonus += emotions.energy * 0.3f;
//                     bonus += (1 - emotions.stress) * 0.2f;
//                     bonus += emotions.confidence * 0.2f;
//                     break;
//             }
            
//             // Personality influences
//             bonus += (extraversion - 0.5f) * 0.1f; // More extraverted agents are slightly more motivated
//             bonus -= neuroticism * 0.2f; // More neurotic agents are less motivated
//             bonus += conscientiousness * 0.2f; // More conscientious agents are more motivated
            
//             return Mathf.Clamp01(bonus);
//         }
//     }
// }