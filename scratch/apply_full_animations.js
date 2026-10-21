const fs = require('fs');

// ==========================================
// 1. UPDATE MainWindow.xaml.cs
// ==========================================
let mwcs = fs.readFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs', 'utf8');

// Replace any existing OnSpinWheelAnimation block
if (mwcs.includes('viewModel.OnSpinWheelAnimation =')) {
  mwcs = mwcs.replace(/viewModel\.OnSpinWheelAnimation =[\s\S]*?await tcs\.Task;\s*\};/, '');
}

const hookTarget = 'viewModel.OnSoundMuteChanged = muted => AudioService.IsMuted = muted;';
const newHook = `viewModel.OnSoundMuteChanged = muted => AudioService.IsMuted = muted;
        viewModel.OnSpinWheelAnimation = async targetAngle =>
        {
            var tcs = new TaskCompletionSource<bool>();
            Dispatcher.Invoke(() =>
            {
                var rotate = (RotateTransform)WheelContainer.RenderTransform;
                var anim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = rotate.Angle,
                    To = targetAngle,
                    Duration = TimeSpan.FromMilliseconds(3200),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                anim.Completed += (s, e) =>
                {
                    rotate.Angle = targetAngle;
                    tcs.TrySetResult(true);
                };

                var tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
                int ticks = 0;
                tickTimer.Tick += (s, e) =>
                {
                    ticks++;
                    if (ticks > 15 || !viewModel.IsSpinning) { tickTimer.Stop(); return; }
                    AudioService.Play("click");
                    if (ticks > 10) tickTimer.Interval = TimeSpan.FromMilliseconds(280);
                };
                tickTimer.Start();

                rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
            });
            await tcs.Task;
        };`;

mwcs = mwcs.replace(hookTarget, newHook);
fs.writeFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml.cs', mwcs, 'utf8');
console.log('MainWindow.xaml.cs updated!');

// ==========================================
// 2. UPDATE MainWindow.xaml
// ==========================================
let xaml = fs.readFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml', 'utf8');

// Remove any existing modal if already inserted
if (xaml.includes('x:Name="WheelContainer"')) {
  // already has it, nothing to remove
} else {
  const luckyModalXaml = `
   <!-- Modal Vòng Quay May Mắn Cyberpunk Gacha Wheel (Toàn màn hình) -->
   <Border Background="#E6050A15" Visibility="{Binding IsLuckyWheelOpen, Converter={StaticResource BoolVisible}}"
           Grid.RowSpan="4" Panel.ZIndex="3000" VerticalAlignment="Stretch" HorizontalAlignment="Stretch">
    <Border Background="#0C152B" BorderBrush="#00F2FE" BorderThickness="2" CornerRadius="24"
            Padding="36,24" MaxWidth="540" HorizontalAlignment="Center" VerticalAlignment="Center">
     <Border.Effect>
      <DropShadowEffect Color="#00F2FE" BlurRadius="30" ShadowDepth="0" Opacity="0.65"/>
     </Border.Effect>
     <StackPanel HorizontalAlignment="Center">
      <!-- Modal Header -->
      <DockPanel Margin="0,0,0,16" Width="460">
       <StackPanel DockPanel.Dock="Left">
        <TextBlock Text="🎡 VÒNG QUAY MAY MẮN CYBERPUNK" Foreground="#00F2FE" FontSize="18" FontWeight="Bold"/>
        <TextBlock Text="Thử vận may mỗi ngày · Nhận quà tặng và giờ chơi giá trị" Foreground="#94A3B8" FontSize="11.5" Margin="0,2,0,0"/>
       </StackPanel>
       <Button DockPanel.Dock="Right" Command="{Binding CloseLuckyWheelCommand}" HorizontalAlignment="Right"
               Background="Transparent" BorderThickness="0" Foreground="#A8BFDF" Cursor="Hand" Padding="4">
        <md:PackIcon Kind="Close" Width="22" Height="22"/>
       </Button>
      </DockPanel>

      <!-- The Wheel Visual -->
      <Grid Width="280" Height="280" Margin="0,8,0,16">
       <!-- Rotating Container Border -->
       <Border x:Name="WheelContainer" Width="280" Height="280" CornerRadius="140"
               BorderBrush="#00F2FE" BorderThickness="3.5" Background="#080E1F" ClipToBounds="True"
               RenderTransformOrigin="0.5,0.5">
        <Border.RenderTransform>
         <RotateTransform Angle="{Binding WheelAngle}"/>
        </Border.RenderTransform>
        <Border.Effect>
         <DropShadowEffect Color="#00F2FE" BlurRadius="18" ShadowDepth="0" Opacity="0.75"/>
        </Border.Effect>
        <Grid>
         <!-- 8 Radially positioned prize sectors -->
         <!-- 0 deg: Top -->
         <StackPanel VerticalAlignment="Top" HorizontalAlignment="Center" Margin="0,18,0,0">
          <md:PackIcon Kind="ClockFast" Width="22" Height="22" Foreground="#00F2FE" HorizontalAlignment="Center"/>
          <TextBlock Text="+30 Phút" FontSize="10.5" FontWeight="Bold" Foreground="#00F2FE" HorizontalAlignment="Center"/>
         </StackPanel>
         <!-- 45 deg: Top-Right -->
         <StackPanel VerticalAlignment="Top" HorizontalAlignment="Right" Margin="0,46,40,0" RenderTransformOrigin="0.5,0.5">
          <StackPanel.RenderTransform><RotateTransform Angle="45"/></StackPanel.RenderTransform>
          <md:PackIcon Kind="BottleSodaClassic" Width="22" Height="22" Foreground="#FF5C8A" HorizontalAlignment="Center"/>
          <TextBlock Text="Sting Dâu" FontSize="10.5" FontWeight="Bold" Foreground="#FF5C8A" HorizontalAlignment="Center"/>
         </StackPanel>
         <!-- 90 deg: Right -->
         <StackPanel VerticalAlignment="Center" HorizontalAlignment="Right" Margin="0,0,16,0" RenderTransformOrigin="0.5,0.5">
          <StackPanel.RenderTransform><RotateTransform Angle="90"/></StackPanel.RenderTransform>
          <md:PackIcon Kind="CashPlus" Width="22" Height="22" Foreground="#FFD700" HorizontalAlignment="Center"/>
          <TextBlock Text="+10.000 ₫" FontSize="10.5" FontWeight="Bold" Foreground="#FFD700" HorizontalAlignment="Center"/>
         </StackPanel>
         <!-- 135 deg: Bottom-Right -->
         <StackPanel VerticalAlignment="Bottom" HorizontalAlignment="Right" Margin="0,0,40,46" RenderTransformOrigin="0.5,0.5">
          <StackPanel.RenderTransform><RotateTransform Angle="135"/></StackPanel.RenderTransform>
          <md:PackIcon Kind="FoodHotDog" Width="22" Height="22" Foreground="#FFA040" HorizontalAlignment="Center"/>
          <TextBlock Text="Xúc Xích" FontSize="10.5" FontWeight="Bold" Foreground="#FFA040" HorizontalAlignment="Center"/>
         </StackPanel>
         <!-- 180 deg: Bottom -->
         <StackPanel VerticalAlignment="Bottom" HorizontalAlignment="Center" Margin="0,0,0,18" RenderTransformOrigin="0.5,0.5">
          <StackPanel.RenderTransform><RotateTransform Angle="180"/></StackPanel.RenderTransform>
          <md:PackIcon Kind="TicketPercent" Width="22" Height="22" Foreground="#A78BFA" HorizontalAlignment="Center"/>
          <TextBlock Text="Giảm 20%" FontSize="10.5" FontWeight="Bold" Foreground="#A78BFA" HorizontalAlignment="Center"/>
         </StackPanel>
         <!-- 225 deg: Bottom-Left -->
         <StackPanel VerticalAlignment="Bottom" HorizontalAlignment="Left" Margin="40,0,0,46" RenderTransformOrigin="0.5,0.5">
          <StackPanel.RenderTransform><RotateTransform Angle="225"/></StackPanel.RenderTransform>
          <md:PackIcon Kind="StarShooting" Width="22" Height="22" Foreground="#38BDF8" HorizontalAlignment="Center"/>
          <TextBlock Text="x2 EXP" FontSize="10.5" FontWeight="Bold" Foreground="#38BDF8" HorizontalAlignment="Center"/>
         </StackPanel>
         <!-- 270 deg: Left -->
         <StackPanel VerticalAlignment="Center" HorizontalAlignment="Left" Margin="16,0,0,0" RenderTransformOrigin="0.5,0.5">
          <StackPanel.RenderTransform><RotateTransform Angle="270"/></StackPanel.RenderTransform>
          <md:PackIcon Kind="GiftOutline" Width="22" Height="22" Foreground="#4ADE80" HorizontalAlignment="Center"/>
          <TextBlock Text="+1 Giờ" FontSize="10.5" FontWeight="Bold" Foreground="#4ADE80" HorizontalAlignment="Center"/>
         </StackPanel>
         <!-- 315 deg: Top-Left -->
         <StackPanel VerticalAlignment="Top" HorizontalAlignment="Left" Margin="40,46,0,0" RenderTransformOrigin="0.5,0.5">
          <StackPanel.RenderTransform><RotateTransform Angle="315"/></StackPanel.RenderTransform>
          <md:PackIcon Kind="ShieldCrown" Width="22" Height="22" Foreground="#F43F5E" HorizontalAlignment="Center"/>
          <TextBlock Text="Cyber Pro" FontSize="10.5" FontWeight="Bold" Foreground="#F43F5E" HorizontalAlignment="Center"/>
         </StackPanel>
        </Grid>
       </Border>

       <!-- Center Glowing Hub -->
       <Border Width="58" Height="58" CornerRadius="29" Background="#112242" BorderBrush="#FFD700" BorderThickness="2.5"
               HorizontalAlignment="Center" VerticalAlignment="Center">
        <Border.Effect>
         <DropShadowEffect Color="#FFD700" BlurRadius="12" ShadowDepth="0" Opacity="0.8"/>
        </Border.Effect>
        <md:PackIcon Kind="Creation" Width="28" Height="28" Foreground="#FFD700" HorizontalAlignment="Center" VerticalAlignment="Center"/>
       </Border>

       <!-- Top Pointer Arrow (Points down to winning sector) -->
       <Polygon Points="132,0 148,0 140,20" Fill="#FFD700" Stroke="#FFA000" StrokeThickness="1.5"
                HorizontalAlignment="Center" VerticalAlignment="Top" Margin="0,-4,0,0">
        <Polygon.Effect>
         <DropShadowEffect Color="#FFD700" BlurRadius="10" ShadowDepth="0" Opacity="0.9"/>
        </Polygon.Effect>
       </Polygon>
      </Grid>

      <!-- Prize Announcement Banner (shown when won) -->
      <Border Visibility="{Binding HasWonPrize, Converter={StaticResource BoolVisible}}"
              Background="#122A40" BorderBrush="#FFD700" BorderThickness="1.5" CornerRadius="12"
              Padding="18,10" Margin="0,0,0,14" HorizontalAlignment="Stretch">
       <Border.Effect>
        <DropShadowEffect Color="#FFD700" BlurRadius="16" ShadowDepth="0" Opacity="0.7"/>
       </Border.Effect>
       <StackPanel HorizontalAlignment="Center">
        <TextBlock Text="🎉 XIN CHÚC MỪNG QUÝ KHÁCH ĐÃ TRÚNG!" Foreground="#FFD700" FontWeight="Bold" FontSize="12.5" HorizontalAlignment="Center"/>
        <TextBlock Text="{Binding WonPrizeName}" Foreground="#55F2A3" FontWeight="Bold" FontSize="15" HorizontalAlignment="Center" Margin="0,4,0,0"/>
       </StackPanel>
      </Border>

      <!-- Action Controls -->
      <TextBlock Text="{Binding LuckySpins, StringFormat='🎁 Còn lại: {0} lượt quay may mắn hôm nay'}"
                 Foreground="#A0B8DE" FontSize="12" FontWeight="SemiBold" HorizontalAlignment="Center" Margin="0,0,0,12"/>

      <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
       <Button Content="🚀 BẮT ĐẦU QUAY" Command="{Binding SpinWheelCommand}"
               IsEnabled="{Binding IsSpinning, Converter={StaticResource BoolVisible}, ConverterParameter=Inverse}"
               Style="{StaticResource CyberButton}" MinWidth="160" Height="42" Margin="0,0,10,0"/>
       <Button Content="Đóng" Command="{Binding CloseLuckyWheelCommand}"
               IsEnabled="{Binding IsSpinning, Converter={StaticResource BoolVisible}, ConverterParameter=Inverse}"
               Style="{StaticResource Secondary}" MinWidth="90" Height="42"/>
      </StackPanel>
     </StackPanel>
    </Border>
   </Border>
  </Grid>
 </Window>`;

  xaml = xaml.replace(/<\/Grid>\s*<\/Window>/, luckyModalXaml);
}

fs.writeFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml', xaml, 'utf8');
console.log('MainWindow.xaml updated!');
