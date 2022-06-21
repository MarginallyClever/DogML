using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Neuron {
    public Axon[] axons = new Axon[9 * 3];
    public float signal=0;
    public float threshold=0;

    public Neuron() {
        for (int i = 0; i < axons.Length; ++i) {
            axons[i] = new Axon();
        }
    }
}