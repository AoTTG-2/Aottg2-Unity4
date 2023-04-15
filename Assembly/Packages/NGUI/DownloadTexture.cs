




using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(UITexture))]
public class DownloadTexture : MonoBehaviour
{
    private Material mMat;
    private Texture2D mTex;
    public string url = "http://www.tasharen.com/misc/logo.png";

    private void OnDestroy()
    {
        if (this.mMat != null)
        {
            UnityEngine.Object.Destroy(this.mMat);
        }
        if (this.mTex != null)
        {
            UnityEngine.Object.Destroy(this.mTex);
        }
    }

    [DebuggerHidden]
    private IEnumerator Start()
    {
        return new StartcIterator7 { fthis = this };
    }

    [CompilerGenerated]
    private sealed class StartcIterator7 : IEnumerator, IDisposable, IEnumerator<object>
    {
        internal object Scurrent;
        internal int SPC;
        internal DownloadTexture fthis;
        internal UITexture ut1;
        internal WWW www0;

        [DebuggerHidden]
        public void Dispose()
        {
            this.SPC = -1;
        }

        public bool MoveNext()
        {
            uint num = (uint) this.SPC;
            this.SPC = -1;
            switch (num)
            {
                case 0:
                    this.www0 = new WWW(this.fthis.url);
                    this.Scurrent = this.www0;
                    this.SPC = 1;
                    return true;

                case 1:
                    this.fthis.mTex = this.www0.texture;
                    if (this.fthis.mTex == null)
                    {
                        goto Label_0118;
                    }
                    this.ut1 = this.fthis.GetComponent<UITexture>();
                    if (this.ut1.material != null)
                    {
                        this.fthis.mMat = new Material(this.ut1.material);
                        break;
                    }
                    this.fthis.mMat = new Material(Shader.Find("Unlit/Transparent Colored"));
                    break;

                default:
                    goto Label_012A;
            }
            this.ut1.material = this.fthis.mMat;
            this.fthis.mMat.mainTexture = this.fthis.mTex;
            this.ut1.MakePixelPerfect();
        Label_0118:
            this.www0.Dispose();
            this.SPC = -1;
        Label_012A:
            return false;
        }

        [DebuggerHidden]
        public void Reset()
        {
            throw new NotSupportedException();
        }

        object IEnumerator<object>.Current
        {
            [DebuggerHidden]
            get
            {
                return this.Scurrent;
            }
        }

        object IEnumerator.Current
        {
            [DebuggerHidden]
            get
            {
                return this.Scurrent;
            }
        }
    }
}

