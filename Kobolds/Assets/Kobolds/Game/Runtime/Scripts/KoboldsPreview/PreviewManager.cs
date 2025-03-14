﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using Febucci.UI;
using UnityEngine.Localization;
using UnityEngine.Serialization;

namespace P3T.Scripts.KoboldsPreview
{
	public class PreviewManager : MonoBehaviour
	{
		[FormerlySerializedAs("animationList")] [SerializeField] private List<string> AnimationList = new ()
		{
			"kobold_A_Pose",
			"kobold_Idle_1",
			"kobold_Idle_2",
			"kobold_Walk_1",
			"kobold_Run_1",
		};

		[FormerlySerializedAs("_shapeKeyList")] [SerializeField] private List<string> ShapeKeyList = new ()
		{
			"Eyes_Annoyed",
			"Eyes_Blink",
			"Eyes_Cry",
			"Eyes_Dead",
			"Eyes_Excited",
			"Eyes_Happy",
			"Eyes_LookDown",
			"Eyes_LookIn",
			"Eyes_LookOut",
			"Eyes_LookUp",
			"Eyes_Rabid",
			"Eyes_Sad",
			"Eyes_Shrink",
			"Eyes_Sleep",
			"Eyes_Spin",
			"Eyes_Squint",
			"Eyes_Trauma",
			"Sweat_L",
			"Sweat_R",
			"Teardrop_L",
			"Teardrop_R"
		};

		[SerializeField] private GameObject[] Kobolds;
		
		[SerializeField] private List<LocalizedString> Descriptions = new();
		
		[Header("UI")]
		[SerializeField] private Dropdown DropdownKobold;
		[SerializeField] private Dropdown DropdownAnimation;
		[SerializeField] private Dropdown DropdownShapeKey;
		[SerializeField] private TypewriterByCharacter DescriptionText;

		private int _koboldIndex;
		
		private Sequence _slideInSequence;

		private bool ChangingKobolds => _slideInSequence.IsActive() || DescriptionText.isShowingText;
		
		void Start()
		{
			List<string> koboldsList = new List<string>();

			for (int i = 0; i < Kobolds.Length; i++)
			{
				string n = Kobolds[i].name;
				koboldsList.Add(n);

				if (i == 0)
					SlideInKobold(Kobolds[i], false);
				else
					Kobolds[i].SetActive(false);
			}

			DropdownKobold.AddOptions(koboldsList);
			DropdownAnimation.AddOptions(AnimationList);
			DropdownShapeKey.AddOptions(ShapeKeyList);

			// Set to Eyes_Blink
			DropdownShapeKey.value = 1;
			ChangeShapeKey();
		}

		void Update()
		{
			if (Input.GetKeyDown("left"))
			{
				PrevKobold();
			}
			else if (Input.GetKeyDown("right"))
			{
				NextKobold();
			}
			else if (Input.GetKeyDown("up")
					&& (Input.GetKey(KeyCode.LeftControl)
						|| Input.GetKey(KeyCode.RightControl)))
			{
				NextShapeKey();
			}
			else if (Input.GetKeyDown("down")
					&& (Input.GetKey(KeyCode.LeftControl)
						|| Input.GetKey(KeyCode.RightControl)))
			{
				PrevShapeKey();
			}
			else if (Input.GetKeyDown("up"))
			{
				NextAnimation();
			}
			else if (Input.GetKeyDown("down"))
			{
				PrevAnimation();
			}
		}


		public void NextKobold()
		{
			if (ChangingKobolds)
				return;

			if (DropdownKobold.value >= DropdownKobold.options.Count - 1)
				DropdownKobold.value = 0;
			else
				DropdownKobold.value++;

			ChangeKobold(true);
		}

		public void PrevKobold()
		{
			if (ChangingKobolds)
				return;
			
			if (DropdownKobold.value <= 0)
				DropdownKobold.value = DropdownKobold.options.Count - 1;
			else
				DropdownKobold.value--;

			ChangeKobold(false);
		}

		private void ChangeKobold(bool isNext)
		{
			SlideOutKobold(Kobolds[_koboldIndex], isNext);
			SlideInKobold(Kobolds[DropdownKobold.value], isNext);
			
			_koboldIndex = DropdownKobold.value;

			ChangeAnimation();
			ChangeShapeKey();
		}

		private void SlideOutKobold(GameObject kobold, bool isNext)
		{
			DescriptionText.StartDisappearingText();
			kobold.transform.DOMove(new Vector3(0, 0, isNext ? -5f :5f), 1f)
				.OnComplete(() => kobold.SetActive(false));
		}

		private void SlideInKobold(GameObject kobold, bool isNext)
		{
			kobold.SetActive(true);
			kobold.transform.position = new Vector3(0, 0, isNext ? 5f :-5f);
			kobold.transform.DOMove(Vector3.zero, 1f)
				.OnComplete(() => DescriptionText.ShowText(Descriptions[_koboldIndex].GetLocalizedString()));
		}

		public void NextAnimation()
		{
			if (DropdownAnimation.value >= DropdownAnimation.options.Count - 1)
				DropdownAnimation.value = 0;
			else
				DropdownAnimation.value++;

			ChangeAnimation();
		}


		public void PrevAnimation()
		{
			if (DropdownAnimation.value <= 0)
				DropdownAnimation.value = DropdownAnimation.options.Count - 1;
			else
				DropdownAnimation.value--;

			ChangeAnimation();
		}

		public void ChangeAnimation()
		{
			Animator animator = Kobolds[DropdownKobold.value].GetComponentInChildren<Animator>();
			if (animator != null)
			{
				int index = DropdownAnimation.value;

				// If Spin/Splash animation
				if (index == 15)
				{
					if (animator.HasState(0, Animator.StringToHash("Spin")))
					{
						animator.Play("Spin");
					}
					else if (animator.HasState(0, Animator.StringToHash("Splash")))
					{
						animator.Play("Splash");
					}
				}
				else
				{
					animator.Play(DropdownAnimation.options[index].text);
				}
			}
		}

		public void NextShapeKey()
		{
			if (DropdownShapeKey.value >= DropdownShapeKey.options.Count - 1)
				DropdownShapeKey.value = 0;
			else
				DropdownShapeKey.value++;

			ChangeShapeKey();
		}

		public void PrevShapeKey()
		{
			if (DropdownShapeKey.value <= 0)
				DropdownShapeKey.value = DropdownShapeKey.options.Count - 1;
			else
				DropdownShapeKey.value--;

			ChangeShapeKey();
		}

		public void ChangeShapeKey()
		{
			Animator animator = Kobolds[DropdownKobold.value].GetComponentInChildren<Animator>();
			if (animator != null)
			{
				animator.Play(DropdownShapeKey.options[DropdownShapeKey.value].text);
			}
		}
	}
}
