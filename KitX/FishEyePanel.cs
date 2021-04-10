using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace KitX
{
    /// <summary>
    /// FishEyePanel
    /// </summary>

    public class FishEyePanel : Panel
    {
        enum AnimateState { None, Up, Down };

        public FishEyePanel()
        {
            this.Background = Brushes.Transparent;
            this.MouseMove += new MouseEventHandler(FishEyePanel_MouseMove);
            this.MouseEnter += new MouseEventHandler(FishEyePanel_MouseEnter);
            this.MouseLeave += new MouseEventHandler(FishEyePanel_MouseLeave);
        }

        public double Magnification
        {
            get { return (double)GetValue(MagnificationProperty); }
            set { SetValue(MagnificationProperty, value); }
        }

        public static readonly DependencyProperty MagnificationProperty =
            DependencyProperty.Register("Magnification", typeof(double), typeof(FishEyePanel), new UIPropertyMetadata(2d));

        public int AnimationMilliseconds
        {
            get { return (int)GetValue(AnimationMillisecondsProperty); }
            set { SetValue(AnimationMillisecondsProperty, value); }
        }

        public static readonly DependencyProperty AnimationMillisecondsProperty =
            DependencyProperty.Register("AnimationMilliseconds", typeof(int), typeof(FishEyePanel), new UIPropertyMetadata(125));

        public bool ScaleToFit
        {
            get { return (bool)GetValue(ScaleToFitProperty); }
            set { SetValue(ScaleToFitProperty, value); }
        }

        public static readonly DependencyProperty ScaleToFitProperty =
            DependencyProperty.Register("ScaleToFit", typeof(bool), typeof(FishEyePanel), new UIPropertyMetadata(true));

        private bool animating = false;
        private Size ourSize;
        private double totalChildWidth = 0;
        private bool wasMouseOver = false;

        void FishEyePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!animating)
                this.InvalidateArrange();
        }

        void FishEyePanel_MouseEnter(object sender, MouseEventArgs e)
        {
            this.InvalidateArrange();
        }

        void FishEyePanel_MouseLeave(object sender, MouseEventArgs e)
        {
            this.InvalidateArrange();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size idealSize = new Size(0, 0);

            Size size = new Size(Double.PositiveInfinity, Double.PositiveInfinity);
            foreach (UIElement child in Children)
            {
                child.Measure(size);
                idealSize.Width += child.DesiredSize.Width;
                idealSize.Height = Math.Max(idealSize.Height, child.DesiredSize.Height);
            }

            if (double.IsInfinity(availableSize.Height) || double.IsInfinity(availableSize.Width))
                return idealSize;
            else
                return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Children == null || this.Children.Count == 0)
                return finalSize;

            ourSize = finalSize;
            totalChildWidth = 0;

            foreach (UIElement child in this.Children)
            {

                if (child.RenderTransform as TransformGroup == null)
                {
                    child.RenderTransformOrigin = new Point(0, 0.5);
                    TransformGroup group = new TransformGroup();
                    child.RenderTransform = group;
                    group.Children.Add(new ScaleTransform());
                    group.Children.Add(new TranslateTransform());

                }

                child.Arrange(new Rect(0, 0, child.DesiredSize.Width, child.DesiredSize.Height));
                totalChildWidth += child.DesiredSize.Width;
            }

            AnimateAll();

            return finalSize;
        }

        void AnimateAll()
        {
            if (this.Children == null || this.Children.Count == 0)
                return;

            animating = true;

            double childWidth = ourSize.Width / this.Children.Count;

            double overallScaleFactor = ourSize.Width / totalChildWidth;

            UIElement prevChild = null;
            UIElement theChild = null;
            UIElement nextChild = null;

            double widthSoFar = 0;
            double theChildX = 0;
            double ratio = 0;

            if (this.IsMouseOver)
            {
                double x = Mouse.GetPosition(this).X;
                foreach (UIElement child in this.Children)
                {
                    if (theChild == null)
                        theChildX = widthSoFar;
                    widthSoFar += (ScaleToFit ? childWidth : child.DesiredSize.Width * overallScaleFactor);
                    if (x < widthSoFar && theChild == null)
                        theChild = child;
                    if (theChild == null)
                        prevChild = child;
                    if (nextChild == null && theChild != child && theChild != null)
                    {
                        nextChild = child;
                        break;
                    }
                }
                if (theChild != null)
                    ratio = (x - theChildX) / (ScaleToFit ? childWidth : (theChild.DesiredSize.Width * overallScaleFactor));    // Range 0-1 of where the mouse is inside the child
            }

            double mag = Magnification;
            double extra = 0;
            if (theChild != null)
                extra += (mag - 1);

            if (prevChild == null)
                extra += (ratio * (mag - 1));
            else if (nextChild == null)
                extra += ((mag - 1) * (1 - ratio));
            else
                extra += (mag - 1);

            double prevScale = this.Children.Count * (1 + ((mag - 1) * (1 - ratio))) / (this.Children.Count + extra);
            double theScale = (mag * this.Children.Count) / (this.Children.Count + extra);
            double nextScale = this.Children.Count * (1 + ((mag - 1) * ratio)) / (this.Children.Count + extra);
            double otherScale = this.Children.Count / (this.Children.Count + extra);

            if (!ScaleToFit && this.IsMouseOver)
            {
                double bigWidth = 0;
                double actualWidth = 0;
                if (prevChild != null)
                {
                    bigWidth += prevScale * prevChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += prevChild.DesiredSize.Width;
                }
                if (theChild != null)
                {
                    bigWidth += theScale * theChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += theChild.DesiredSize.Width;
                }
                if (nextChild != null)
                {
                    bigWidth += nextScale * nextChild.DesiredSize.Width * overallScaleFactor;
                    actualWidth += nextChild.DesiredSize.Width;
                }
                double w = (totalChildWidth - actualWidth) * overallScaleFactor * otherScale;
                otherScale *= (ourSize.Width - bigWidth) / w;
            }

            widthSoFar = 0;
            double duration = 0;
            if (wasMouseOver != this.IsMouseOver)
                duration = AnimationMilliseconds;

            foreach (UIElement child in this.Children)
            {
                double scale = otherScale;
                if (child == prevChild)
                {
                    scale = prevScale;
                }
                else if (child == theChild)
                {
                    scale = theScale;
                }
                else if (child == nextChild)
                {
                    scale = nextScale;
                }

                if (ScaleToFit)
                {

                    scale *= childWidth / child.DesiredSize.Width;
                }
                else
                {
                    // Apply overall scale so all children fit our width
                    scale *= overallScaleFactor;
                }

                AnimateTo(child, 0, widthSoFar, (ourSize.Height - child.DesiredSize.Height) / 2, scale, duration);
                widthSoFar += child.DesiredSize.Width * scale;
            }

            wasMouseOver = this.IsMouseOver;
        }

        private void AnimateTo(UIElement child, double r, double x, double y, double s, double duration)
        {
            TransformGroup group = (TransformGroup)child.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            TranslateTransform trans = (TranslateTransform)group.Children[1];
//            RotateTransform rot = (RotateTransform)group.Children[2];

            if (duration == 0)
            {
                trans.BeginAnimation(TranslateTransform.XProperty, null);
                trans.BeginAnimation(TranslateTransform.YProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
//                rot.BeginAnimation(RotateTransform.AngleProperty, null);
                trans.X = x;
                trans.Y = y;
                scale.ScaleX = s;
                scale.ScaleY = s;
//                rot.AngleProperty = r;
                animation_Completed(null, null);
            }
            else
            {
                trans.BeginAnimation(TranslateTransform.XProperty, MakeAnimation(x, duration, animation_Completed));
                trans.BeginAnimation(TranslateTransform.YProperty, MakeAnimation(y, duration));
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, MakeAnimation(s, duration));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, MakeAnimation(s, duration));
//                rot.BeginAnimation(RotateTransform.AngleProperty, MakeAnimation(r, duration));
            }
        }

        private DoubleAnimation MakeAnimation(double to, double duration)
        {
            return MakeAnimation(to, duration, null);
        }

        private DoubleAnimation MakeAnimation(double to, double duration, EventHandler endEvent)
        {
            DoubleAnimation anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(duration));
            anim.AccelerationRatio = 0.2;
            anim.DecelerationRatio = 0.7;
            if (endEvent != null)
                anim.Completed += endEvent;
            return anim;
        }

        void animation_Completed(object sender, EventArgs e)
        {
            animating = false;
        }
    }
}