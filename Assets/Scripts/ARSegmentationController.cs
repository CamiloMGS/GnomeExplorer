using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Niantic.ARDK;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.VirtualStudio.AR.Mock;
using Debug = UnityEngine.Debug;
using System.Diagnostics;
using TMPro;

public class ARSegmentationController : MonoBehaviour
{

    public static ARSegmentationController instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    [SerializeField] private ARSessionManager ARSessionManager = null;
    [SerializeField] private ARSemanticSegmentationManager semanticSegmentationManager;
    [SerializeField] private Shader segmentationShader;
    [SerializeField] private Canvas canvas;
    [SerializeField] private int increment = 2;
    [SerializeField] private int evaluateTime = 500;
    [SerializeField] private int timeCollection;

    [Header("Debug")]
    [SerializeField] private TextMeshProUGUI[] texts = new TextMeshProUGUI[6];
    public enum Channels
    {
        sky,
        ground,
        artificial_ground,
        water,
        building,
        foliage,
        none
    }

    public Channels channelInfo;

    [Serializable]
    public struct Segmentation
    {
        public Channels ChannelType;

        public Texture2D Texture;
    }

    public Segmentation[] Segmentations;
    public Channels oldChanel = Channels.none;

    private int pixelsTotalX;
    private int pixelsTotalY;
    public float percent;
    public bool isCollecting;
    private Stopwatch watchTimer = new Stopwatch();
    private Stopwatch watchCollection = new Stopwatch();
    private Texture2D[] mask = new Texture2D[8];
    private RawImage[] rawImages = new RawImage[8];


    void Start()
    {
        InitializeARDK();

        watchTimer.Start();

        CreateUISegmentation();
    }

    
    private void InitializeARDK() 
    {
        ARSessionManager.EnableFeatures();
        semanticSegmentationManager.SemanticBufferUpdated += OnSemanticBufferUpdated;
        ARSessionFactory.SessionInitialized += OnSessionInitialized;

    }

    private void CreateUISegmentation() 
    {
        int index = 0;
        foreach (Segmentation segm in Segmentations)
        {
            string channelName = segm.ChannelType.ToString().ToLower();

            RawImage segmentationOverlay = new GameObject(channelName + "Segmentation").AddComponent<RawImage>();
            segmentationOverlay.transform.SetParent(canvas.transform);
            segmentationOverlay.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 0);
            segmentationOverlay.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
            segmentationOverlay.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 0);
            segmentationOverlay.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
            segmentationOverlay.rectTransform.anchorMin = new Vector2(0, 0);
            segmentationOverlay.rectTransform.anchorMax = new Vector2(1, 1);
            segmentationOverlay.transform.localScale = new Vector3(1, 1, 1);

            Material mat = new Material(segmentationShader);
            segmentationOverlay.material = mat;

            rawImages[index] = segmentationOverlay;
            index++;
        }
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
        Resolution resolution = new Resolution();
        resolution.width = Screen.width;
        resolution.height = Screen.height;
        ARSessionFactory.SessionInitialized -= OnSessionInitialized;
    }

    private void OnSemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
    {
        ISemanticBuffer semanticBuffer = args.Sender.AwarenessBuffer;

        if (channelInfo != oldChanel)
        {
            foreach (Transform child in canvas.transform)
            {
                if (child.name != channelInfo.ToString() + "Segmentation")
                {
                    child.gameObject.SetActive(false);
                }
                else
                {
                    child.gameObject.SetActive(true);
                    watchCollection.Reset();
                    isCollecting = false;
                }

            }

            oldChanel = channelInfo;
        }


        int channelIndex = semanticBuffer.GetChannelIndex(channelInfo.ToString());

        semanticSegmentationManager.SemanticBufferProcessor.CopyToAlignedTextureARGB32
        (
            texture: ref mask[channelIndex],
            channel: channelIndex,
            orientation: Screen.orientation
        );

        rawImages[channelIndex].material.SetTexture("_Mask", mask[channelIndex]);
        rawImages[channelIndex].material.SetTexture("_Tex", Segmentations[channelIndex].Texture);

        if (watchTimer.ElapsedMilliseconds > evaluateTime)
        {
            EvaluateSegmentationTexture(mask[channelIndex]);
        }

        if (GamePlayManager.instance.shouldCollect)
        {
            if (percent > GamePlayManager.instance.conditions[channelIndex].RequirementToCollect && !isCollecting && !watchCollection.IsRunning)
            {
                GamePlayManager.instance.StartCollect(channelInfo.ToString());
                watchCollection.Start();
                isCollecting = true;
                ParticleSystemManager.instance.ActivatePS(channelIndex);
            }
            else if (percent < GamePlayManager.instance.conditions[channelIndex].RequirementToCollect && watchCollection.IsRunning)
            {
                watchCollection.Stop();
                isCollecting = false;
            }
            if (watchCollection.IsRunning)
            {
                var currentCollection = watchCollection.ElapsedMilliseconds;
                if (currentCollection >= GamePlayManager.instance.conditions[channelIndex].QuantityToCollect)
                {
                    watchCollection.Stop();
                    watchCollection.Reset();
                    isCollecting = false;
                    GamePlayManager.instance.shouldCollect = false;
                    GamePlayManager.instance.FinishCollect(channelInfo.ToString());
                    percent = 0;
                }
            }
        }
        //Debug 

        ShowDebug(channelIndex);
    }

    public void EvaluateSegmentationTexture(Texture2D segmentationTexture)
    {
        pixelsTotalX = 0;
        pixelsTotalY = 0;
        int count = 0;
        int y = 0;
        while (y < segmentationTexture.height)
        {
            int x = 0;
            while (x < segmentationTexture.width)
            {

                if (segmentationTexture.GetPixel(x, y) == Color.white)
                {
                    count++;
                }
                x = increment + x;
                pixelsTotalX++;
            }
            y = increment + y;
            pixelsTotalY++;
        }

        int totalPixels = pixelsTotalX + pixelsTotalY;
        float percentTemp = (float)count / (float)totalPixels;
        float finalValue = (float)Math.Round(percentTemp, 4);
        percent = finalValue * 100;
        watchTimer.Reset();
        watchTimer.Start();
    }

    public void ChangeChannel(int channel) 
    {
        switch (channel)
        {
            case 0:
                channelInfo = Channels.sky;
                Debug.Log("Need to collec: " + channelInfo);
                break;
            case 1:
                channelInfo = Channels.ground;
                Debug.Log("Need to collec: " + channelInfo);
                break;
            case 2:
                channelInfo = Channels.artificial_ground;
                Debug.Log("Need to collec: " + channelInfo);
                break;
            case 3:
                channelInfo = Channels.water;
                Debug.Log("Need to collec: " + channelInfo);
                break;
            case 4:
                channelInfo = Channels.building;
                Debug.Log("Need to collec: " + channelInfo);
                break;
            case 5:
                channelInfo = Channels.foliage;
                Debug.Log("Need to collec: " + channelInfo);
                break;
            default:
                break;
        }
    }

    public void ShowDebug(int index)
    {
        texts[0].text = "I Need: " + channelInfo.ToString() + ": " + GamePlayManager.instance.conditions[index].RequirementToCollect;
/*        texts[1].text = "Requeriment to start collecting: " + GamePlayManager.instance.conditions[index].RequirementToCollect;
        texts[2].text = "Available: " + channelInfo + " " + percent;*/
        texts[3].text = "Quantity To Collect:  " + GamePlayManager.instance.conditions[index].QuantityToCollect;
        var currentCollection = watchCollection.ElapsedMilliseconds;
        texts[1].text = currentCollection.ToString();
        texts[5].text = "Is Collecting: " + isCollecting;
    }
}
