// BreadcrumbBar.cs
// F14 — Menús más Accesibles y Reorganizados
// Barra de navegación de grupos tipo breadcrumb.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using KeePassLib;

namespace KeePass.UI.Accessibility
{
	/// <summary>
	/// Barra de navegación de grupos (breadcrumb).
	/// Muestra la ruta del grupo seleccionado: Todas > Email > Google.
	/// Se actualiza llamando a <see cref="UpdatePath"/>.
	/// </summary>
	public sealed class BreadcrumbBar : UserControl
	{
		private readonly FlowLayoutPanel m_panel;

		// Evento que se dispara cuando el usuario hace clic en un segmento.
		public event EventHandler<BreadcrumbClickEventArgs> BreadcrumbClick;

		public BreadcrumbBar()
		{
			m_panel = new FlowLayoutPanel
			{
				Dock = DockStyle.Fill,
				FlowDirection = FlowDirection.LeftToRight,
				WrapContents = false,
				AutoScroll = false,
				AutoSize = false,
				BackColor = Color.Transparent,
				Padding = new Padding(2, 1, 2, 1)
			};

			this.Controls.Add(m_panel);
			this.Height = 20;
			this.Dock = DockStyle.Top;
			this.BackColor = SystemColors.Control;
			this.BorderStyle = BorderStyle.None;
		}

		/// <summary>
		/// Actualiza la barra con un grupo y construye la ruta hasta la raíz.
		/// </summary>
		public void UpdateFromGroup(PwGroup pg)
		{
			if(pg == null) { Clear(); return; }

			List<PwGroup> path = new List<PwGroup>();
			PwGroup cur = pg;
			while(cur != null)
			{
				path.Insert(0, cur);
				cur = cur.ParentGroup;
			}

			string[] segments = new string[path.Count];
			for(int i = 0; i < path.Count; i++)
				segments[i] = path[i].Name;

			// Guardar referencia a los grupos para el clic
			UpdatePath(segments, path.ToArray());
		}

		/// <summary>
		/// Actualiza la barra con un array de segmentos de texto.
		/// </summary>
		public void UpdatePath(string[] segments)
		{
			UpdatePath(segments, null);
		}

		private void UpdatePath(string[] segments, PwGroup[] groups)
		{
			m_panel.SuspendLayout();
			m_panel.Controls.Clear();

			if(segments == null || segments.Length == 0)
			{
				m_panel.ResumeLayout(true);
				return;
			}

			for(int i = 0; i < segments.Length; i++)
			{
				int idx = i; // captura para lambda
				PwGroup grp = (groups != null && i < groups.Length) ? groups[i] : null;

				if(i > 0)
				{
					Label sep = new Label
					{
						Text = " > ",
						AutoSize = true,
						ForeColor = SystemColors.GrayText,
						Padding = new Padding(0),
						Margin = new Padding(0)
					};
					m_panel.Controls.Add(sep);
				}

				string seg = segments[i];
				if(i == segments.Length - 1)
				{
					// Último segmento: solo texto (no enlazable)
					Label lbl = new Label
					{
						Text = seg,
						AutoSize = true,
						ForeColor = SystemColors.ControlText,
						Font = new Font(this.Font, FontStyle.Bold),
						Padding = new Padding(0),
						Margin = new Padding(0)
					};
					m_panel.Controls.Add(lbl);
				}
				else
				{
					LinkLabel link = new LinkLabel
					{
						Text = seg,
						AutoSize = true,
						Padding = new Padding(0),
						Margin = new Padding(0)
					};
					link.LinkBehavior = LinkBehavior.HoverUnderline;
					PwGroup capturedGrp = grp;
					string capturedSeg = seg;
					int capturedIdx = idx;
					link.LinkClicked += delegate(object sender, LinkLabelLinkClickedEventArgs e)
					{
						if(BreadcrumbClick != null)
							BreadcrumbClick(this, new BreadcrumbClickEventArgs(capturedIdx, capturedSeg, capturedGrp));
					};
					m_panel.Controls.Add(link);
				}
			}

			m_panel.ResumeLayout(true);
		}

		public void Clear()
		{
			m_panel.Controls.Clear();
		}
	}

	public sealed class BreadcrumbClickEventArgs : EventArgs
	{
		public int SegmentIndex { get; private set; }
		public string SegmentName { get; private set; }
		public PwGroup Group { get; private set; }

		public BreadcrumbClickEventArgs(int index, string name, PwGroup group)
		{
			SegmentIndex = index;
			SegmentName = name;
			Group = group;
		}
	}
}
