using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReelControll : MonoBehaviour
{
    public RenderTexture[] reelTexture;
    public RenderTexture[] reelEmissonTexture;
    public Texture2D[] symbols;
    public Texture2D reelBackWhite;
    public Texture2D reelBackBlack;
    public GameObject[] reels = new GameObject[3];

    private float speedAddAmount = 0.52f;
    private float[] rollSpeeds = new float[3] { 0, 0, 0 };
    private bool[] isRolls = new bool[3] { false, false, false };
    private float[] rotate = new float[3] { 0, 0, 0 };

    private int[,] symbolIndexes = new int[3,20];

    // Start is called before the first frame update
    void Start()
    {
        symbolIndexes = new int[3, 20]{
            {
                4,0,1,3,2,4,0,1,3,2,4,0,1,3,2,4,0,1,3,2
            },
            {
                1,0,4,2,3,1,0,4,2,3,1,0,4,2,3,1,0,4,2,3
            },
            {
                1,0,4,3,2,1,0,4,3,2,1,0,4,3,2,1,0,4,3,2
            },
        };
    }

    void DrawTextureOnRenderTexture(Texture2D sourceTexture, int x, int y, RenderTexture renderTexture)
    {
        // 現在のRenderTextureを保存
        RenderTexture currentRT = RenderTexture.active;

        // 作成したRenderTextureをアクティブに設定
        RenderTexture.active = renderTexture;

        // 画像描画の準備
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);

        // 画像を描画
        Graphics.DrawTexture(new Rect(x, y, sourceTexture.width, sourceTexture.height), sourceTexture);
        GL.PopMatrix();

        // RenderTextureを元に戻す
        RenderTexture.active = currentRT;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.X))
        {
            RollStart(0);
            RollStart(1);
            RollStart(2);
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            RollStop(0);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            RollStop(1);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            RollStop(2);
        }

        RotateUpdate(0);
        RotateUpdate(1);
        RotateUpdate(2);
        reelDrawUpdate(0);
        reelDrawUpdate(1);
        reelDrawUpdate(2);
    }

    private void RotateUpdate(int speedIndex)
    {
        if (!isRolls[speedIndex])
        {
            return;
        }
        if (rollSpeeds[speedIndex] > 8)
        {
            rollSpeeds[speedIndex] = 8;
        }
        else
        {
            rollSpeeds[speedIndex] += speedAddAmount;
        }
        reels[speedIndex].transform.Rotate(new Vector3(1, 0, 0), -rollSpeeds[speedIndex]);
    }

    private void reelDrawUpdate(int index)
    {
        int cellHeight = reelTexture[index].height / 20;
        DrawTextureOnRenderTexture(reelBackWhite, 0, 0, reelTexture[index]);
        for (int symbolI = 0; symbolI < 20; symbolI++)
        {
            Texture2D cSymbol = symbols[symbolIndexes[index,symbolI]];
            DrawTextureOnRenderTexture(
                cSymbol,
                (reelTexture[index].width / 2) - (cSymbol.width / 2),
                (cellHeight / 2) - (cSymbol.height / 2) + symbolI * (cellHeight),
                reelTexture[index]);
        }
        DrawTextureOnRenderTexture(reelBackBlack, 0, 0, reelEmissonTexture[index]);
        for (int symbolI = 0; symbolI < 20; symbolI++)
        {
            Texture2D cSymbolMasks = symbols[symbolIndexes[index,symbolI]];
            DrawTextureOnRenderTexture(
                cSymbolMasks,
                (reelTexture[index].width / 2) - (cSymbolMasks.width / 2),
                (cellHeight / 2) - (cSymbolMasks.height / 2) + symbolI * (cellHeight),
                reelEmissonTexture[index]);
        }
    }

    private void RollStart(int reelIndex)
    {
        isRolls[reelIndex] = true;
    }

    private void RollStop(int reelIndex)
    {
        isRolls[reelIndex] = false;
        rollSpeeds[reelIndex] = 0;
    }
}
