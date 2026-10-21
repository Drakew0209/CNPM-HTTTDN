const fs = require('fs');

// 1. Update App.xaml
let appXaml = fs.readFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/App.xaml', 'utf8');

const oldFoodCardBorder = appXaml.match(/<Style x:Key="FoodCardBorder"[\s\S]*?<\/Style>/)[0];
const newFoodCardBorder = `  <Style x:Key="FoodCardBorder" TargetType="Border">
    <Setter Property="Background" Value="#0D162C"/>
    <Setter Property="BorderBrush" Value="#1E3156"/>
    <Setter Property="BorderThickness" Value="1.5"/>
    <Setter Property="CornerRadius" Value="16"/>
    <Setter Property="Width" Value="210"/>
    <Setter Property="Margin" Value="0,0,16,18"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="RenderTransformOrigin" Value="0.5,0.5"/>
    <Setter Property="RenderTransform">
      <Setter.Value>
        <ScaleTransform ScaleX="1.0" ScaleY="1.0"/>
      </Setter.Value>
    </Setter>
    <Setter Property="Effect">
      <Setter.Value>
        <DropShadowEffect Color="#000000" BlurRadius="12" ShadowDepth="4" Opacity="0.45"/>
      </Setter.Value>
    </Setter>
    <Style.Triggers>
      <Trigger Property="IsMouseOver" Value="True">
        <Setter Property="BorderBrush" Value="#00F2FE"/>
        <Setter Property="Background" Value="#122244"/>
        <Setter Property="Effect">
          <Setter.Value>
            <DropShadowEffect Color="#00F2FE" BlurRadius="22" ShadowDepth="0" Opacity="0.75"/>
          </Setter.Value>
        </Setter>
        <Trigger.EnterActions>
          <BeginStoryboard>
            <Storyboard>
              <DoubleAnimation Storyboard.TargetProperty="RenderTransform.(ScaleTransform.ScaleX)" To="1.035" Duration="0:0:0.18">
                <DoubleAnimation.EasingFunction><QuadraticEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
              </DoubleAnimation>
              <DoubleAnimation Storyboard.TargetProperty="RenderTransform.(ScaleTransform.ScaleY)" To="1.035" Duration="0:0:0.18">
                <DoubleAnimation.EasingFunction><QuadraticEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
              </DoubleAnimation>
            </Storyboard>
          </BeginStoryboard>
        </Trigger.EnterActions>
        <Trigger.ExitActions>
          <BeginStoryboard>
            <Storyboard>
              <DoubleAnimation Storyboard.TargetProperty="RenderTransform.(ScaleTransform.ScaleX)" To="1.0" Duration="0:0:0.18">
                <DoubleAnimation.EasingFunction><QuadraticEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
              </DoubleAnimation>
              <DoubleAnimation Storyboard.TargetProperty="RenderTransform.(ScaleTransform.ScaleY)" To="1.0" Duration="0:0:0.18">
                <DoubleAnimation.EasingFunction><QuadraticEase EasingMode="EaseOut"/></DoubleAnimation.EasingFunction>
              </DoubleAnimation>
            </Storyboard>
          </BeginStoryboard>
        </Trigger.ExitActions>
      </Trigger>
    </Style.Triggers>
  </Style>`;

appXaml = appXaml.replace(oldFoodCardBorder, newFoodCardBorder);

const oldCyberButton = appXaml.match(/<Style x:Key="CyberButton"[\s\S]*?<\/Style>/)[0];
const newCyberButton = `  <Style x:Key="CyberButton" TargetType="Button">
    <Setter Property="Foreground" Value="#080D1B"/>
    <Setter Property="FontWeight" Value="Bold"/>
    <Setter Property="FontSize" Value="13.5"/>
    <Setter Property="Cursor" Value="Hand"/>
    <Setter Property="MinHeight" Value="42"/>
    <Setter Property="Padding" Value="18,8"/>
    <Setter Property="Template">
      <Setter.Value>
        <ControlTemplate TargetType="Button">
          <Border x:Name="BtnBorder" CornerRadius="12" Padding="{TemplateBinding Padding}">
            <Border.Background>
              <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                <GradientStop Color="#00F2FE" Offset="0"/>
                <GradientStop Color="#0077FE" Offset="1"/>
              </LinearGradientBrush>
            </Border.Background>
            <Border.Effect>
              <DropShadowEffect x:Name="BtnShadow" Color="#00F2FE" BlurRadius="12" ShadowDepth="0" Opacity="0.5"/>
            </Border.Effect>
            <Border.Triggers>
              <EventTrigger RoutedEvent="Border.Loaded">
                <BeginStoryboard>
                  <Storyboard RepeatBehavior="Forever" AutoReverse="True">
                    <DoubleAnimation Storyboard.TargetName="BtnShadow" Storyboard.TargetProperty="BlurRadius" From="10" To="20" Duration="0:0:1.6">
                      <DoubleAnimation.EasingFunction><SineEase EasingMode="EaseInOut"/></DoubleAnimation.EasingFunction>
                    </DoubleAnimation>
                    <DoubleAnimation Storyboard.TargetName="BtnShadow" Storyboard.TargetProperty="Opacity" From="0.45" To="0.85" Duration="0:0:1.6">
                      <DoubleAnimation.EasingFunction><SineEase EasingMode="EaseInOut"/></DoubleAnimation.EasingFunction>
                    </DoubleAnimation>
                  </Storyboard>
                </BeginStoryboard>
              </EventTrigger>
            </Border.Triggers>
            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property="IsMouseOver" Value="True">
              <Setter TargetName="BtnBorder" Property="Effect">
                <Setter.Value>
                  <DropShadowEffect Color="#00F2FE" BlurRadius="26" ShadowDepth="0" Opacity="0.95"/>
                </Setter.Value>
              </Setter>
            </Trigger>
            <Trigger Property="IsPressed" Value="True">
              <Setter TargetName="BtnBorder" Property="Opacity" Value="0.85"/>
            </Trigger>
            <Trigger Property="IsEnabled" Value="False">
              <Setter TargetName="BtnBorder" Property="Opacity" Value="0.3"/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>`;

appXaml = appXaml.replace(oldCyberButton, newCyberButton);

fs.writeFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/App.xaml', appXaml, 'utf8');
console.log('App.xaml updated successfully!');
