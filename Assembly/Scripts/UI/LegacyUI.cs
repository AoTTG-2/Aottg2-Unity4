using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class LegacyPopupTemplate
{
    Texture2D BorderTexture;
    Texture2D BackgroundTexture;
    float PositionX;
    float PositionY;
    float Width;
    float Height;
    float BorderThickness;
    float Padding;
    Color ButtonColor;
    public LegacyPopupTemplate(Texture2D borderTexture, Texture2D bgTexture, Color buttonColor, float x, float y, float w, float h, float borderThickness)
    {
        BorderTexture = borderTexture;
        BackgroundTexture = bgTexture;
        ButtonColor = buttonColor;
        PositionX = x - (w / 2f);
        PositionY = y - (h / 2f);
        Width = w;
        Height = h;
        BorderThickness = borderThickness;
    }

    public void DrawPopup(string message, float messageWidth, float messageHeight)
    {
        DrawPopupBackground();
        float widthPadding = (Width - messageWidth) * 0.5f;
        float heightPadding = (Height - messageHeight) * 0.5f;
        GUI.Label(new Rect(PositionX + widthPadding, PositionY + heightPadding, messageWidth, messageHeight), message);
    }

    public bool DrawPopupWithButton(string message, float messageWidth, float messageHeight, string buttonMessage, float buttonWidth, float buttonHeight)
    {
        DrawPopupBackground();
        float messageWidthPadding = (Width - messageWidth) * 0.5f;
        float buttonWidthPadding = (Width - buttonWidth) * 0.5f;
        float heightPadding = (Height - messageHeight - buttonHeight) / 3f;
        GUI.Label(new Rect(PositionX + messageWidthPadding, PositionY + heightPadding, messageWidth, messageHeight), message);
        float buttonX = PositionX + buttonWidthPadding;
        float buttonY = PositionY + Height - buttonHeight - heightPadding;
        GUI.backgroundColor = ButtonColor;
        return GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), buttonMessage);
    }

    public bool[] DrawPopupWithTwoButtons(string message, float messageWidth, float messageHeight, string button1Message, float button1Width, string button2Message,
        float button2Width, float buttonHeight)
    {
        DrawPopupBackground();
        float messageWidthPadding = (Width - messageWidth) * 0.5f;
        float buttonWidthPadding = (Width - button1Width - button2Width) / 3f;
        float heightPadding = (Height - messageHeight - buttonHeight) / 3f;
        GUI.Label(new Rect(PositionX + messageWidthPadding, PositionY + heightPadding, messageWidth, messageHeight), message);
        float button1X = PositionX + buttonWidthPadding;
        float button2X = button1X + button1Width + buttonWidthPadding;
        float buttonY = PositionY + Height - buttonHeight - heightPadding;
        GUI.backgroundColor = ButtonColor;
        return new bool[2] { GUI.Button(new Rect(button1X, buttonY, button1Width, buttonHeight), button1Message),
        GUI.Button(new Rect(button2X, buttonY, button2Width, buttonHeight), button2Message) };
    }

    void DrawPopupBackground()
    {
        GUI.DrawTexture(new Rect(PositionX, PositionY, Width, Height), BorderTexture);
        GUI.DrawTexture(new Rect(PositionX + BorderThickness, PositionY + BorderThickness, Width - 2 * BorderThickness, Height - 2 * BorderThickness), BackgroundTexture);
    }
}
