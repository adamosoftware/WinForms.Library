﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;
using WinForms.Library.Extensions;
using WinForms.Library.Interfaces;
using WinForms.Library.Models;

namespace WinForms.Library
{
	public class ControlBinder<TDocument>
	{
		private bool _dirty = false;
		private bool _suspend = false;
		private List<Action<TDocument>> _setControls = new List<Action<TDocument>>();
		private List<Action> _clearControls = new List<Action>();

		public TDocument Document { get; set; }

		public event EventHandler IsDirtyChanged;

		public bool IsDirty
		{
			get { return _dirty; }
			set
			{
				if (_dirty != value)
				{
					_dirty = value;
					IsDirtyChanged?.Invoke(this, new EventArgs());
				}
			}
		}

		public void ClearValues()
		{
			_suspend = true;
			foreach (var action in _clearControls) action.Invoke();
			_suspend = false;
			IsDirty = false;
		}

		public void LoadValues()
		{
			_suspend = true;
			foreach (var setter in _setControls) setter.Invoke(Document);
			_suspend = false;
			IsDirty = false;
		}

		#region TextBox

		public void Add(TextBox control, Action<TDocument> setProperty, Action<TDocument> setControl)
		{
			_setControls.Add(setControl);

			control.TextChanged += delegate (object sender, EventArgs e)
			{
				if (_suspend) return;
				setProperty.Invoke(Document);
				IsDirty = true;
			};
		}

		public void Add(TextBox control, Expression<Func<TDocument, object>> property)
		{
			PropertyInfo pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				pi.SetValue(doc, control.Text);
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{
				control.Text = func.Invoke(doc)?.ToString();
			};

			Add(control, setProperty, setControl);
		}

		#endregion TextBox

		#region CheckBox

		public void Add(CheckBox control, Action<TDocument> setProperty, Action<TDocument> setControl)
		{
			_setControls.Add(setControl);

			control.CheckedChanged += delegate (object sender, EventArgs e)
			{
				if (_suspend) return;
				setProperty.Invoke(Document);
				IsDirty = true;
			};
		}

		public void Add(CheckBox control, Expression<Func<TDocument, bool>> property)
		{
			PropertyInfo pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				pi.SetValue(doc, control.Checked);
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{
				control.Checked = func.Invoke(doc);
			};

			Add(control, setProperty, setControl);
		}

		#endregion CheckBox

		#region ComboBox

		public void AddEnum<TEnum>(ComboBox control, Action<TDocument> setProperty, Action<TDocument> setControl)
		{
			control.Fill<TEnum>();

			_clearControls.Add(() => control.SelectedIndex = -1);
			_setControls.Add(setControl);

			control.SelectedIndexChanged += delegate (object sender, EventArgs e)
			{
				if (_suspend) return;
				setProperty.Invoke(Document);
				IsDirty = true;
			};
		}

		public void AddEnum<TEnum>(ComboBox control, Expression<Func<TDocument, TEnum>> property)
		{
			PropertyInfo pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				pi.SetValue(doc, (control.SelectedItem as ListItem<TEnum>).Value);
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{
				ComboBoxExtensions.SetValue(control, func.Invoke(doc));
			};

			AddEnum<TEnum>(control, setProperty, setControl);
		}

		public void AddItems<TItem>(ComboBox control, Expression<Func<TDocument, TItem>> property, IEnumerable<TItem> items) where TItem : class
		{
			PropertyInfo pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				pi.SetValue(doc, (control.SelectedItem as TItem));
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{
				ComboBoxExtensions.SetItem(control, func.Invoke(doc));
			};

			AddItems(control, setProperty, setControl, items);
		}

		public void AddItems<TItem>(ComboBox control, Action<TDocument> setProperty, Action<TDocument> setControl, IEnumerable<TItem> items)
		{
			control.Fill(items);

			_clearControls.Add(() => control.SelectedIndex = -1);
			_setControls.Add(setControl);

			control.SelectedIndexChanged += delegate (object sender, EventArgs e)
			{
				if (_suspend) return;
				setProperty.Invoke(Document);
				IsDirty = true;
			};
		}

		/// <summary>
		/// Fills a combo box from TItem keys of a dictionary, mapping to corresponding TValues that are bound to the document		
		/// </summary>
		public void AddItems<TValue, TItem>(ComboBox control, Expression<Func<TDocument, TValue>> property, Dictionary<TValue, TItem> itemDictionary) where TItem : class
		{			
			var pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				var reverseDictionary = itemDictionary.ToDictionary(kp => kp.Value, kp => kp.Key);
				pi.SetValue(doc, reverseDictionary[control.GetItem<TItem>()]);
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{
				var value = func.Invoke(doc);				
				ComboBoxExtensions.SetItem(control, itemDictionary[value]);
			};

			AddItems(control, setProperty, setControl, itemDictionary.Select(kp => kp.Value));
		}

		#endregion

		#region DateTimePicker

		public void Add(DateTimePicker control, Action<TDocument> setProperty, Action<TDocument> setControl)
		{
			_setControls.Add(setControl);

			control.ValueChanged += delegate (object sender, EventArgs e)
			{
				if (_suspend) return;
				setProperty.Invoke(Document);
				IsDirty = true;
			};
		}

		public void Add(DateTimePicker control, Expression<Func<TDocument, DateTime>> property)
		{
			PropertyInfo pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				pi.SetValue(doc, control.Value);
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{
				DateTime value = func.Invoke(doc);
				if (value < DateTimePicker.MinimumDateTime) value = DateTimePicker.MinimumDateTime;
				control.Value = value;
			};

			Add(control, setProperty, setControl);
		}

		#endregion

		#region NumericUpDate

		public void Add(NumericUpDown control, Action<TDocument> setProperty, Action<TDocument> setControl)
		{
			_setControls.Add(setControl);

			control.ValueChanged += delegate (object sender, EventArgs e)
			{
				if (_suspend) return;
				setProperty.Invoke(Document);
				IsDirty = true;
			};
		}

		public void Add(NumericUpDown control, Expression<Func<TDocument, decimal>> property)
		{
			PropertyInfo pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				pi.SetValue(doc, control.Value);
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{				
				control.Value = func.Invoke(doc);
			};

			Add(control, setProperty, setControl);
		}

		#endregion

		#region IBoundControl

		public void Add<TValue>(IBoundControl<TValue> control, Action<TDocument> setProperty, Action<TDocument> setControl)
		{
			_setControls.Add(setControl);

			control.ValueChanged += delegate (object sender, EventArgs e)
			{
				if (_suspend) return;
				setProperty.Invoke(Document);
				IsDirty = true;
			};
		}

		public void Add<TValue>(IBoundControl<TValue> control, Expression<Func<TDocument, TValue>> property)
		{
			PropertyInfo pi = GetProperty(property);
			Action<TDocument> setProperty = (doc) =>
			{
				pi.SetValue(doc, control.Value);
			};

			var func = property.Compile();
			Action<TDocument> setControl = (doc) =>
			{
				control.Value = func.Invoke(doc);
			};

			Add(control, setProperty, setControl);
		}

		#endregion IBoundControl

		#region support methods

		private PropertyInfo GetProperty<TValue>(Expression<Func<TDocument, TValue>> property)
		{
			string propName = PropertyNameFromLambda(property);
			PropertyInfo pi = typeof(TDocument).GetProperty(propName);
			return pi;
		}

		private string PropertyNameFromLambda(Expression expression)
		{
			// thanks to http://odetocode.com/blogs/scott/archive/2012/11/26/why-all-the-lambdas.aspx
			// thanks to http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

			LambdaExpression le = expression as LambdaExpression;
			if (le == null) throw new ArgumentException("expression");

			MemberExpression me = null;
			if (le.Body.NodeType == ExpressionType.Convert)
			{
				me = ((UnaryExpression)le.Body).Operand as MemberExpression;
			}
			else if (le.Body.NodeType == ExpressionType.MemberAccess)
			{
				me = le.Body as MemberExpression;
			}

			if (me == null) throw new ArgumentException("expression");

			return me.Member.Name;
		}

		#endregion support methods
	}
}