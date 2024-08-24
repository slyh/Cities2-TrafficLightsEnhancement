using System;
using System.Linq;
using C2VM.CommonLibraries.LaneSystem;
using Colossal.Randomization;
using Game.Areas;
using Game.Net;
using System.Collections.Generic;
using Game.Prefabs;
using HarmonyLib;
using Unity.Collections;
using Unity.Entities;

namespace C2VM.TrafficLightsEnhancement.Systems.UISystem;

[HarmonyPatch]
class Patches
{
    [HarmonyPatch(typeof(Colossal.Localization.LocalizationManager), "NotifyActiveDictionaryChanged")]
    [HarmonyPostfix]
    static void NotifyActiveDictionaryChanged()
    {
        C2VM.TrafficLightsEnhancement.Systems.UISystem.UISystem.UpdateLocale();
    }

    [HarmonyPatch(typeof(Game.Tools.NetToolSystem), "SetAppliedUpgrade")]
    [HarmonyPostfix]
    static void NetToolSystemSetAppliedUpgrade(Game.Tools.NetToolSystem __instance, bool removing)
    {
        if (removing)
        {
            return;
        }

        Entity entity = Traverse.Create(__instance).Field("m_AppliedUpgrade").Property("value").Field("m_Entity").GetValue<Entity>();
        CompositionFlags flags = Traverse.Create(__instance).Field("m_AppliedUpgrade").Property("value").Field("m_Flags").GetValue<CompositionFlags>();

        if ((flags.m_General & CompositionFlags.General.TrafficLights) != 0)
        {
            UISystem uiSystem = __instance.World.GetOrCreateSystemManaged<UISystem>();
            uiSystem.ChangeSelectedEntity(entity);

            Random rand = new Random();
            BufferLookup<Game.Net.SubLane> subLaneLookup = __instance.GetBufferLookup<Game.Net.SubLane>(isReadOnly: false);
            ComponentLookup<Components.TestComponent> testComponentLookup = __instance.GetComponentLookup<Components.TestComponent>(isReadOnly: true);
            ComponentLookup<Game.Net.Edge> edgeLookup = __instance.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
            ComponentLookup<Game.Net.Node> nodeLookup = __instance.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
            ComponentLookup<Game.Net.EdgeGeometry> edgeGeometryLookup = __instance.GetComponentLookup<Game.Net.EdgeGeometry>(isReadOnly: true);
            ComponentLookup<Game.Common.Owner> ownerLookup = __instance.GetComponentLookup<Game.Common.Owner>(isReadOnly: true);
            ComponentLookup<Game.Net.Curve> curveLookup = __instance.GetComponentLookup<Game.Net.Curve>(isReadOnly: true);
            ComponentLookup<Game.Net.MasterLane> masterLaneLookup = __instance.GetComponentLookup<Game.Net.MasterLane>(isReadOnly: true);
            ComponentLookup<Game.Net.Lane> laneLookup = __instance.GetComponentLookup<Lane>(isReadOnly: true);
            ComponentLookup<Game.Net.PedestrianLane> pedestrianLaneLookup = __instance.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
            System.Console.WriteLine($"Node {entity}");
            // if (subLaneLookup.HasBuffer(entity))
            // {
            //     for (int i = 0; i < subLaneLookup[entity].Length; i++)
            //     {
            //         Entity subLane = subLaneLookup[entity][i].m_SubLane;
            //         // if (masterLaneLookup.HasComponent(subLane))
            //         // {
            //         //     System.Console.WriteLine($"SubLane (MasterLane) {subLane}");
            //         //     if (laneLookup.HasComponent(subLane))
            //         //     {
            //         //         System.Console.WriteLine($"Lane m_StartNode {laneLookup[subLane].m_StartNode.GetHashCode()} m_MiddleNode {laneLookup[subLane].m_MiddleNode.GetHashCode()} m_EndNode {laneLookup[subLane].m_EndNode.GetHashCode()}");
            //         //     }
            //         //     // NativeArray<ComponentType> componentTypes = __instance.EntityManager.GetChunk(subLane).Archetype.GetComponentTypes(Allocator.Temp);
            //         //     // for (int j = 0; j < componentTypes.Length; j++)
            //         //     // {
            //         //     //     System.Console.WriteLine($"ComponentType {componentTypes[j].GetManagedType().Name}");
            //         //     // }
            //         //     // componentTypes.Dispose();
            //         // }
            //         if (pedestrianLaneLookup.HasComponent(subLane))
            //         {
            //             System.Console.WriteLine($"SubLane (PedestrianLane) {subLane} m_Flags {pedestrianLaneLookup[subLane].m_Flags}");
            //             if (laneLookup.HasComponent(subLane))
            //             {
            //                 System.Console.WriteLine($"Lane m_StartNode {laneLookup[subLane].m_StartNode.GetHashCode()} m_MiddleNode {laneLookup[subLane].m_MiddleNode.GetHashCode()} m_EndNode {laneLookup[subLane].m_EndNode.GetHashCode()}");
            //             }
            //         }
            //     }
            // }
            // if (subLaneLookup.HasBuffer(entity))
            // {
            //     for (int i = 0; i < subLaneLookup[entity].Length; i++)
            //     {
            //         Entity subLane = subLaneLookup[entity][i].m_SubLane;
            //         if (masterLaneLookup.HasComponent(subLane))
            //         {
            //             System.Console.WriteLine($"SubLane {subLane}");
            //             // if (ownerLookup.HasComponent(subLane))
            //             // {
            //             //     System.Console.WriteLine($"Owner {ownerLookup[subLane].m_Owner}");
            //             // }
            //             if (curveLookup.HasComponent(subLane))
            //             {
            //                 System.Console.WriteLine($"Curve a {curveLookup[subLane].m_Bezier.a} b {curveLookup[subLane].m_Bezier.a} c {curveLookup[subLane].m_Bezier.a} d {curveLookup[subLane].m_Bezier.a}");
            //             }
            //         }
            //     }
            // }
            // if (subLaneLookup.HasBuffer(entity))
            // {
            //     for (int i = 0; i < subLaneLookup[entity].Length; i++)
            //     {
            //         Entity subLane = subLaneLookup[entity][i].m_SubLane;
            //         if (masterLaneLookup.HasComponent(subLane))
            //         {
            //             if (!testComponentLookup.HasComponent(subLane))
            //             {
            //                 Components.TestComponent tc = new Components.TestComponent{
            //                     id = rand.NextLong()
            //                 };
            //                 System.Console.WriteLine($"Creating Components.TestComponent for {subLane} tc = {tc.id}");
            //                 __instance.EntityManager.AddComponentData(subLane, tc);
            //             }
            //             else
            //             {
            //                 Components.TestComponent tc = testComponentLookup[subLane];
            //                 System.Console.WriteLine($"Components.TestComponent {subLane} tc = {tc.id}");
            //             }
            //         }
            //     }
            // }

            BufferLookup<Game.Net.ConnectedEdge> connectedEdgeLookup = __instance.GetBufferLookup<Game.Net.ConnectedEdge>(isReadOnly: false);
            if (connectedEdgeLookup.HasBuffer(entity))
            {
                for (int i = 0; i < connectedEdgeLookup[entity].Length; i++)
                {
                    Entity edgeEntity = connectedEdgeLookup[entity][i].m_Edge;
                    EdgeGeometry edgeGeometry = edgeGeometryLookup[edgeEntity];
                    Edge edge = edgeLookup[edgeEntity];
                    Game.Net.Node nodeStart = nodeLookup[edge.m_Start];
                    Game.Net.Node nodeEnd = nodeLookup[edge.m_End];
                    Game.Net.Node nodeJunction = default;
                    if (edge.m_Start.Equals(entity))
                    {
                        nodeJunction = nodeStart;
                    }
                    if (edge.m_End.Equals(entity))
                    {
                        nodeJunction = nodeEnd;
                    }
                    System.Console.WriteLine($"Edge {edgeEntity}");
                    // if (!testComponentLookup.HasComponent(edgeEntity))
                    // {
                    //     Components.TestComponent tc = new Components.TestComponent{
                    //         id = rand.NextLong()
                    //     };
                    //     System.Console.WriteLine($"Creating Components.TestComponent for {edgeEntity} tc = {tc.id}");
                    //     __instance.EntityManager.AddComponentData(edgeEntity, tc);
                    // }
                    // else
                    // {
                    //     Components.TestComponent tc = testComponentLookup[edgeEntity];
                    //     System.Console.WriteLine($"Components.TestComponent {edgeEntity} tc = {tc.id}");
                    // }
                    System.Console.WriteLine($"m_Start.m_Left a {edgeGeometry.m_Start.m_Left.a} b {edgeGeometry.m_Start.m_Left.b} c {edgeGeometry.m_Start.m_Left.c} d {edgeGeometry.m_Start.m_Left.d}");
                    System.Console.WriteLine($"m_Start.m_Right a {edgeGeometry.m_Start.m_Right.a} b {edgeGeometry.m_Start.m_Right.b} c {edgeGeometry.m_Start.m_Right.c} d {edgeGeometry.m_Start.m_Right.d}");
                    System.Console.WriteLine($"m_End.m_Left a {edgeGeometry.m_End.m_Left.a} b {edgeGeometry.m_End.m_Left.b} c {edgeGeometry.m_End.m_Left.c} d {edgeGeometry.m_End.m_Left.d}");
                    System.Console.WriteLine($"m_End.m_Right a {edgeGeometry.m_End.m_Right.a} b {edgeGeometry.m_End.m_Right.b} c {edgeGeometry.m_End.m_Right.c} d {edgeGeometry.m_End.m_Right.d}");
                    // if (!testComponentLookup.HasComponent(edgeEntity))
                    // {
                    //     Components.TestComponent tc = new Components.TestComponent{
                    //         id = rand.NextLong()
                    //     };
                    //     System.Console.WriteLine($"Creating Components.TestComponent for Edge {edgeEntity} tc = {tc.id}");
                    //     __instance.EntityManager.AddComponentData(edgeEntity, tc);
                    // }
                    // else
                    // {
                    //     Components.TestComponent tc = testComponentLookup[edgeEntity];
                    //     System.Console.WriteLine($"Components.TestComponent Edge {edgeEntity} tc = {tc.id}");
                    // }
                    // if (subLaneLookup.HasBuffer(edgeEntity))
                    // {
                    //     for (int j = 0; j < subLaneLookup[edgeEntity].Length; j++)
                    //     {
                    //         Entity subLane = subLaneLookup[edgeEntity][j].m_SubLane;
                    //         // if (masterLaneLookup.HasComponent(subLane))
                    //         // {
                    //         //     System.Console.WriteLine($"SubLane (MasterLane) {subLane}");
                    //         //     if (laneLookup.HasComponent(subLane))
                    //         //     {
                    //         //         System.Console.WriteLine($"Lane m_StartNode {laneLookup[subLane].m_StartNode.GetHashCode()} m_MiddleNode {laneLookup[subLane].m_MiddleNode.GetHashCode()} m_EndNode {laneLookup[subLane].m_EndNode.GetHashCode()}");
                    //         //     }
                    //         //     // if (curveLookup.HasComponent(subLane))
                    //         //     // {
                    //         //     //     System.Console.WriteLine($"Curve a {curveLookup[subLane].m_Bezier.a} b {curveLookup[subLane].m_Bezier.a} c {curveLookup[subLane].m_Bezier.a} d {curveLookup[subLane].m_Bezier.a}");
                    //         //     // }
                    //         // }
                    //         if (pedestrianLaneLookup.HasComponent(subLane))
                    //         {
                    //             System.Console.WriteLine($"SubLane (PedestrianLane) {subLane} m_Flags {pedestrianLaneLookup[subLane].m_Flags}");
                    //             if (laneLookup.HasComponent(subLane))
                    //             {
                    //                 System.Console.WriteLine($"Lane m_StartNode {laneLookup[subLane].m_StartNode.GetHashCode()} m_MiddleNode {laneLookup[subLane].m_MiddleNode.GetHashCode()} m_EndNode {laneLookup[subLane].m_EndNode.GetHashCode()}");
                    //             }
                    //         }
                    //     }
                    // }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Game.UI.InGame.ToolbarUISystem), "Apply")]
    [HarmonyPostfix]
    static void ToolbarUISystemApply(Game.UI.InGame.ToolbarUISystem __instance, List<Entity> themes, List<Entity> packs, Entity assetMenuEntity, Entity assetCategoryEntity, Entity assetEntity)
    {
        UISystem uiSystem = __instance.World.GetOrCreateSystemManaged<UISystem>();
        if (__instance.EntityManager.HasComponent<PlaceableNetData>(assetEntity))
        {
            PlaceableNetData placeableNetData = __instance.EntityManager.GetComponentData<PlaceableNetData>(assetEntity);
            if ((placeableNetData.m_SetUpgradeFlags.m_General & CompositionFlags.General.TrafficLights) != 0)
            {
                uiSystem.ShouldShowPanel(true);
                return;
            }
        }
        uiSystem.ShouldShowPanel(false);
    }
}