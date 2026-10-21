const fs = require('fs');

let xaml = fs.readFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml', 'utf8');

// Match the entire Hero PC01 Card
const oldCardMatch = xaml.match(/<Border Style="\{StaticResource Card\}" Padding="0"><Grid><Grid\.ColumnDefinitions><ColumnDefinition Width="6"\/><ColumnDefinition\/><ColumnDefinition Width="Auto"\/><\/Grid\.ColumnDefinitions>[\s\S]*?<md:PackIcon Kind="Monitor" Width="72" Height="72" Foreground="#59DFFB" HorizontalAlignment="Center"\/><\/StackPanel><\/Grid><\/Border>/);

if (oldCardMatch) {
  const newCard = `<Border Style="{StaticResource Card}" Padding="0" Background="#0C152B" BorderBrush="#1C2E52" BorderThickness="1.5">
      <Grid>
       <Grid.ColumnDefinitions>
        <ColumnDefinition Width="6"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
       </Grid.ColumnDefinitions>

       <!-- Left Cyan Accent Line -->
       <Border Background="#00F2FE" CornerRadius="16,0,0,16">
        <Border.Effect>
         <DropShadowEffect Color="#00F2FE" BlurRadius="10" ShadowDepth="0" Opacity="0.8"/>
        </Border.Effect>
       </Border>

       <!-- Left Info & Actions -->
       <StackPanel Grid.Column="1" Margin="28,20">
        <!-- Status Badge Row -->
        <StackPanel Orientation="Horizontal" Margin="0,0,0,10">
         <Border Background="#14352E" BorderBrush="#10B981" BorderThickness="1" CornerRadius="8" Padding="10,4" Margin="0,0,10,0">
          <StackPanel Orientation="Horizontal">
           <Border Width="6" Height="6" CornerRadius="3" Background="#10B981" Margin="0,0,6,0" VerticalAlignment="Center">
            <Border.Effect><DropShadowEffect Color="#10B981" BlurRadius="6" ShadowDepth="0" Opacity="0.9"/></Border.Effect>
           </Border>
           <TextBlock Text="ĐANG HOẠT ĐỘNG · BẢO VỆ PHÒNG MÁY" Foreground="#55F2A3" FontSize="11" FontWeight="Bold"/>
          </StackPanel>
         </Border>

         <Border Background="#162E4A" BorderBrush="#00F2FE" BorderThickness="1" CornerRadius="8" Padding="10,4">
          <TextBlock Text="{Binding HourlyRateLabel, StringFormat='🏷️ ĐƠN GIÁ: {0}'}" Foreground="#38D9FF" FontSize="11" FontWeight="Bold"/>
         </Border>
        </StackPanel>

        <!-- Machine Name & Status -->
        <TextBlock Text="{Binding MachineName}" FontSize="36" FontWeight="Bold" Foreground="White" Margin="0,0,0,2"/>
        <TextBlock Text="{Binding SessionStatus}" Foreground="#94A3B8" FontSize="13" Margin="0,0,0,16"/>

        <!-- Action Buttons -->
        <WrapPanel>
         <Button Command="{Binding EndSessionCommand}" IsEnabled="{Binding CanEndSession}"
                 Background="#2E1724" BorderBrush="#EF4444" BorderThickness="1.2" Foreground="#FCA5A5"
                 Cursor="Hand" MinHeight="36" Height="36" Padding="14,0" Margin="0,0,10,0">
          <StackPanel Orientation="Horizontal">
           <md:PackIcon Kind="PowerStandby" Width="16" Height="16" VerticalAlignment="Center" Margin="0,0,6,0"/>
           <TextBlock Text="Kết thúc phiên" FontWeight="SemiBold" FontSize="12.5" VerticalAlignment="Center"/>
          </StackPanel>
         </Button>

         <Button Click="OpenCompact"
                 Background="#132B45" BorderBrush="#00F2FE" BorderThickness="1.2" Foreground="#38D9FF"
                 Cursor="Hand" MinHeight="36" Height="36" Padding="14,0" Margin="0,0,10,0">
          <StackPanel Orientation="Horizontal">
           <md:PackIcon Kind="ArrowCollapseAll" Width="16" Height="16" VerticalAlignment="Center" Margin="0,0,6,0"/>
           <TextBlock Text="Cửa sổ gọn" FontWeight="SemiBold" FontSize="12.5" VerticalAlignment="Center"/>
          </StackPanel>
         </Button>

         <Button Command="{Binding LockTemporaryCommand}" IsEnabled="{Binding HasActiveSession}"
                 Background="#2B2416" BorderBrush="#F59E0B" BorderThickness="1.2" Foreground="#FDE68A"
                 Cursor="Hand" MinHeight="36" Height="36" Padding="14,0">
          <StackPanel Orientation="Horizontal">
           <md:PackIcon Kind="LockClock" Width="16" Height="16" VerticalAlignment="Center" Margin="0,0,6,0"/>
           <TextBlock Text="Treo máy" FontWeight="SemiBold" FontSize="12.5" VerticalAlignment="Center"/>
          </StackPanel>
         </Button>
        </WrapPanel>
       </StackPanel>

       <!-- Right Column: Holographic Gaming Battlestation HUD -->
       <Border Grid.Column="2" Background="#080E1F" BorderBrush="#1C2E52" BorderThickness="1.2" CornerRadius="14"
               Padding="16,12" Margin="14,16,24,16" VerticalAlignment="Center">
        <Border.Effect>
         <DropShadowEffect Color="#00F2FE" BlurRadius="16" ShadowDepth="0" Opacity="0.3"/>
        </Border.Effect>
        <Grid>
         <Grid.ColumnDefinitions>
          <ColumnDefinition Width="Auto"/>
          <ColumnDefinition Width="Auto"/>
         </Grid.ColumnDefinitions>

         <!-- Animated Tech Hologram Avatar with Rotating Ring -->
         <Grid Width="84" Height="84" Margin="0,0,16,0" VerticalAlignment="Center">
          <!-- Outer Rotating Tech Hexagon / Circle -->
          <Ellipse Width="82" Height="82" Stroke="#00F2FE" StrokeThickness="1.5" StrokeDashArray="4 3" RenderTransformOrigin="0.5,0.5">
           <Ellipse.RenderTransform>
            <RotateTransform Angle="0"/>
           </Ellipse.RenderTransform>
           <Ellipse.Triggers>
            <EventTrigger RoutedEvent="Ellipse.Loaded">
             <BeginStoryboard>
              <Storyboard RepeatBehavior="Forever">
               <DoubleAnimation Storyboard.TargetProperty="RenderTransform.(RotateTransform.Angle)"
                                From="0" To="360" Duration="0:0:10"/>
              </Storyboard>
             </BeginStoryboard>
            </EventTrigger>
           </Ellipse.Triggers>
          </Ellipse>

          <!-- Inner Pulsing Glow Background -->
          <Border Width="64" Height="64" CornerRadius="32" Background="#112445" BorderBrush="#254A80" BorderThickness="1.5"
                  HorizontalAlignment="Center" VerticalAlignment="Center">
           <Border.Effect>
            <DropShadowEffect Color="#00F2FE" BlurRadius="14" ShadowDepth="0" Opacity="0.6"/>
           </Border.Effect>
           <Grid>
            <!-- Battlestation Icon -->
            <md:PackIcon Kind="MonitorDashboard" Width="34" Height="34" Foreground="#00F2FE" HorizontalAlignment="Center" VerticalAlignment="Center"/>
            <!-- Little Gamepad Badge at Corner -->
            <Border Background="#A855F7" CornerRadius="7" Width="16" Height="16" HorizontalAlignment="Right" VerticalAlignment="Bottom" Margin="0,0,4,4">
             <md:PackIcon Kind="LightningBolt" Width="10" Height="10" Foreground="White" HorizontalAlignment="Center" VerticalAlignment="Center"/>
            </Border>
           </Grid>
          </Border>
         </Grid>

         <!-- Station Live Hardware Specs -->
         <StackPanel Grid.Column="1" VerticalAlignment="Center" MinWidth="180">
          <StackPanel Orientation="Horizontal" Margin="0,0,0,6">
           <Border Background="#163C2E" BorderBrush="#10B981" BorderThickness="1" CornerRadius="6" Padding="6,2" Margin="0,0,6,0">
            <StackPanel Orientation="Horizontal">
             <Border Width="5" Height="5" CornerRadius="2.5" Background="#10B981" Margin="0,0,4,0" VerticalAlignment="Center"/>
             <TextBlock Text="PRO GAMING RIG" Foreground="#34D399" FontSize="9.5" FontWeight="Bold"/>
            </StackPanel>
           </Border>
           <Border Background="#1E2A4A" CornerRadius="6" Padding="6,2">
            <TextBlock Text="240Hz Fast-IPS" Foreground="#38D9FF" FontSize="9.5" FontWeight="Bold"/>
           </Border>
          </StackPanel>

          <!-- Spec Lines -->
          <Grid Margin="0,2,0,2">
           <Grid.ColumnDefinitions><ColumnDefinition Width="65"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
           <TextBlock Text="Cấu hình:" Foreground="#8FAAC9" FontSize="11"/>
           <TextBlock Grid.Column="1" Text="RTX 4070 · i7 14700F" Foreground="#E2EDF9" FontWeight="SemiBold" FontSize="11"/>
          </Grid>
          <Grid Margin="0,2,0,2">
           <Grid.ColumnDefinitions><ColumnDefinition Width="65"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
           <TextBlock Text="Độ trễ Ping:" Foreground="#8FAAC9" FontSize="11"/>
           <StackPanel Grid.Column="1" Orientation="Horizontal">
            <TextBlock Text="⚡ 1 ms" Foreground="#10B981" FontWeight="Bold" FontSize="11"/>
            <TextBlock Text="(Cực nhạy)" Foreground="#64748B" FontSize="10" Margin="4,0,0,0" VerticalAlignment="Center"/>
           </StackPanel>
          </Grid>
          <Grid Margin="0,2,0,0">
           <Grid.ColumnDefinitions><ColumnDefinition Width="65"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
           <TextBlock Text="Âm thanh:" Foreground="#8FAAC9" FontSize="11"/>
           <TextBlock Grid.Column="1" Text="Cyber SFX Engine" Foreground="#A78BFA" FontWeight="SemiBold" FontSize="11"/>
          </Grid>
         </StackPanel>
        </Grid>
       </Border>
      </Grid>
     </Border>`;

  xaml = xaml.replace(oldCardMatch[0], newCard);
  fs.writeFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml', xaml, 'utf8');
  console.log('Hero Card successfully upgraded in MainWindow.xaml!');
} else {
  console.error('Could not match old Card block!');
}
