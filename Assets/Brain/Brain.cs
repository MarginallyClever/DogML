using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Brain {
    private const int SIZEX = 10;
    private const int SIZEY = 10;
    private const int SIZEZ = 1;
    private Neuron[] neurons = new Neuron[SIZEX * SIZEY * SIZEZ];
    private int biggestInput = 0;

    // Start is called before the first frame update
    public Brain() {
        for (int z = 0; z < SIZEZ; ++z) {
            for (int y = 0; y < SIZEY; ++y) {
                for (int x = 0; x < SIZEX; ++x) {
                    Neuron n = new Neuron();
                    neurons[GetNeuronIndex(x, y, z)] = n;
                    SetupNeuron(n, x, y, z);
                }
            }
        }
    }

    internal void reward(float rewardAmount) {
        //throw new NotImplementedException();
    }

    private void SetupNeuron(Neuron n, int nx, int ny, int nz) {
        for (int z = -1; z < 2; ++z) {
            for (int y = -1; y < 2; ++y) {
                for (int x = -1; x < 2; ++x) {
                    if (x == 0 && y == 0 && z == 0) continue;
                    if (NeuronIndexIsValid(nx + x, ny + y, nz + z)) {
                        Axon a = n.axons[GetAxonIndex(x, y, z)];
                        a.toAddress = GetNeuronIndex(nx, ny, nz);
                        a.weight = 1;
                    }
                }
            }
        }
    }

    private bool NeuronIndexIsValid(int x, int y, int z) {
        if (x < 0 || x >= SIZEX) return false;
        if (y < 0 || y >= SIZEY) return false;
        if (z < 0 || z >= SIZEZ) return false;
        return true;
    }

    private int GetAxonIndex(int x, int y, int z) {
        return ((z + 1) * 3 * 3) + ((y + 1) * 3) + (x + 1);
    }

    private int GetNeuronIndex(int x, int y, int z) {
        return (z * SIZEX * SIZEY) + (y * SIZEX) + x;
    }

    // Update is called once per frame
    public void Update() {
        UpdateAllNeurons();
    }

    private void UpdateAllNeurons() { 
        foreach(Neuron n in neurons) {
            if (n.signal > n.threshold) {
                float pulse = n.signal;
                foreach(Axon a in n.axons) {
                    if (a.toAddress == -1) continue;
                    neurons[a.toAddress].signal += pulse * a.weight;
                }
            }
            n.signal = 0;
        }
    }

    public void input(int v, float signal) {
        neurons[v].signal = signal;
        biggestInput = Math.Max(v, biggestInput);
    }

    public float output(int v) {
        return neurons[biggestInput + v].signal;
    }
}
