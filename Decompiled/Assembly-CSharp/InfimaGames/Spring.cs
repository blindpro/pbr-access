using System;
using UnityEngine;

namespace InfimaGames;

public class Spring
{
	private SpringSettings settings;

	private Vector3 initialVelocity;

	private Vector3 start;

	private Vector3 end;

	private Vector3 currentValue;

	private Vector3 currentVelocity;

	private Vector3 currentAcceleration;

	private float stepSize = 1f / 61f;

	private bool isFirstEvaluate = true;

	private int hFrames;

	private HeldForce heldForce;

	public Spring()
	{
		settings = new SpringSettings
		{
			damping = 16f,
			mass = 1f,
			speed = 1f,
			stiffness = 169f
		};
	}

	public Spring(SpringSettings newSettings)
	{
		settings = newSettings;
	}

	private void Reset()
	{
		currentValue = start;
		currentVelocity = initialVelocity;
		currentAcceleration = Vector3.zero;
	}

	public void Adjust(SpringSettings newSettings)
	{
		settings = newSettings;
	}

	public void UpdateEndValue(Vector3 value)
	{
		UpdateEndValue(value, currentVelocity);
	}

	public void SetHeldForce(HeldForce force)
	{
		heldForce = force;
	}

	public void UpdateEndValue(Vector3 value, Vector3 velocity)
	{
		end = value;
		currentVelocity = velocity;
	}

	public Vector3 Evaluate()
	{
		if (heldForce.Frames > 0)
		{
			hFrames++;
			if (hFrames >= heldForce.Frames)
			{
				hFrames = 0;
				heldForce = default(HeldForce);
			}
		}
		if (isFirstEvaluate)
		{
			Reset();
			isFirstEvaluate = false;
		}
		float num = Time.deltaTime * settings.speed;
		float damping = settings.damping;
		float mass = settings.mass;
		float stiffness = settings.stiffness;
		Vector3 vector = currentValue;
		Vector3 vector2 = currentVelocity;
		Vector3 vector3 = currentAcceleration;
		float num2 = ((num > stepSize) ? stepSize : (num - 0.001f));
		float num3 = Mathf.Ceil(num / num2);
		for (int i = 0; (float)i < num3; i++)
		{
			float num4 = ((Math.Abs((float)i - (num3 - 1f)) < 0.01f) ? (num - (float)i * num2) : num2);
			vector += vector2 * num4 + vector3 * (num4 * num4 * 0.5f);
			Vector3 vector4 = ((0f - stiffness) * (vector - (end + heldForce.Force)) + (0f - damping) * vector2) / mass;
			vector2 += (vector3 + vector4) * (num4 * 0.5f);
			vector3 = vector4;
		}
		currentValue = vector;
		currentVelocity = vector2;
		currentAcceleration = vector3;
		return currentValue;
	}

	public Vector3 Evaluate(SpringSettings newSettings)
	{
		Adjust(newSettings);
		return Evaluate();
	}
}
