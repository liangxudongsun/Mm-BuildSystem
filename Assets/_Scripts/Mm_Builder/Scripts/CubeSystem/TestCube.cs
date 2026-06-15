
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mm_Budier
{
    public class TestCube : CubeBehaviour
    {
        public override void OnPlaced(CubeInstance cubeInstance)
        {
            base.OnPlaced(cubeInstance);
            Debug.Log("OnPlaced: " + cubeInstance.data.CubeType);
        }
        public override void OnRemoved()
        {   
            base.OnRemoved();
            Debug.Log("OnRemoved: " + this.curCubeInstance.data.CubeType);
            this.curCubeInstance = null;
        }
        public override void OnInteract(CubeInstance cubeInstance)
        {
            base.OnInteract(cubeInstance);
            Debug.Log("OnInteract: " + cubeInstance.data.CubeType);
        }
    }
}