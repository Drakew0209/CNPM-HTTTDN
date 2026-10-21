const fs = require('fs');

let xaml = fs.readFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml', 'utf8');

// 1. Upgrade TabItem style in TabControl.Resources
const oldStyleMatch = xaml.match(/<Style TargetType="TabItem" BasedOn="\{StaticResource MaterialDesignTabItem\}">[\s\S]*?<\/Style>/);

if (oldStyleMatch) {
  const newStyle = `<Style TargetType="TabItem">
       <Setter Property="MinWidth" Value="176"/>
       <Setter Property="MinHeight" Value="44"/>
       <Setter Property="Margin" Value="0,0,0,5"/>
       <Setter Property="Foreground" Value="#8FAACD"/>
       <Setter Property="Cursor" Value="Hand"/>
       <Setter Property="Template">
        <Setter.Value>
         <ControlTemplate TargetType="TabItem">
          <Border x:Name="TabBorder" CornerRadius="10" Background="Transparent" Padding="12,9" Margin="0,0,14,0">
           <Grid>
            <!-- Left Neon Accent Bar for Active State -->
            <Border x:Name="ActiveBar" Width="3.2" HorizontalAlignment="Left" CornerRadius="1.6" Background="#00F2FE" Opacity="0" Margin="-12,-2,0,-2">
             <Border.Effect>
              <DropShadowEffect Color="#00F2FE" BlurRadius="8" ShadowDepth="0" Opacity="0.95"/>
             </Border.Effect>
            </Border>
            <ContentPresenter ContentSource="Header" VerticalAlignment="Center" Margin="6,0,0,0"/>
           </Grid>
          </Border>
          <ControlTemplate.Triggers>
           <Trigger Property="IsMouseOver" Value="True">
            <Setter TargetName="TabBorder" Property="Background" Value="#13233E"/>
            <Setter Property="Foreground" Value="#E2EDF9"/>
           </Trigger>
           <Trigger Property="IsSelected" Value="True">
            <Setter TargetName="TabBorder" Property="Background">
             <Setter.Value>
              <LinearGradientBrush StartPoint="0,0" EndPoint="1,0">
               <GradientStop Color="#183158" Offset="0"/>
               <GradientStop Color="#0F1E38" Offset="1"/>
              </LinearGradientBrush>
             </Setter.Value>
            </Setter>
            <Setter TargetName="ActiveBar" Property="Opacity" Value="1"/>
            <Setter Property="Foreground" Value="#00F2FE"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
           </Trigger>
          </ControlTemplate.Triggers>
         </ControlTemplate>
        </Setter.Value>
       </Setter>
      </Style>`;
  xaml = xaml.replace(oldStyleMatch[0], newStyle);
}

// 2. Replace TabItem Headers with Rich Neon Icons & Badges
// Tab 0: Tổng quan
xaml = xaml.replace('<TabItem Header="Tổng quan">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="ViewDashboardOutline" Width="18" Height="18" Foreground="#00F2FE" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <TextBlock Text="Tổng quan" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

// Tab 1: Menu Game
xaml = xaml.replace('<TabItem Header="Menu Game">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="GamepadVariantOutline" Width="18" Height="18" Foreground="#C084FC" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <Border DockPanel.Dock="Right" Background="#B91C1C" CornerRadius="6" Padding="5,1" VerticalAlignment="Center">
        <TextBlock Text="HOT" Foreground="White" FontSize="9" FontWeight="Bold"/>
       </Border>
       <TextBlock Text="Menu Game" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

// Tab 2: Thực đơn
xaml = xaml.replace('<TabItem Header="Thực đơn">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="FoodForkDrink" Width="18" Height="18" Foreground="#FB923C" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <Border DockPanel.Dock="Right" Background="#0284C7" CornerRadius="8" Padding="6,1" VerticalAlignment="Center"
               Visibility="{Binding Cart.Count, Converter={StaticResource EmptyVisible}, ConverterParameter=Inverse}">
        <TextBlock Text="{Binding Cart.Count, StringFormat='🛒 {0}'}" Foreground="White" FontSize="9.5" FontWeight="Bold"/>
       </Border>
       <TextBlock Text="Thực đơn" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

// Tab 3: Đơn của tôi
xaml = xaml.replace('<TabItem Header="Đơn của tôi">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="ReceiptTextOutline" Width="18" Height="18" Foreground="#34D399" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <TextBlock Text="Đơn của tôi" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

// Tab 4: Ví và nạp tiền
xaml = xaml.replace('<TabItem Header="Ví và nạp tiền">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="WalletOutline" Width="18" Height="18" Foreground="#FBBF24" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <Border DockPanel.Dock="Right" Background="#166534" CornerRadius="6" Padding="5,1" VerticalAlignment="Center">
        <TextBlock Text="+KM" Foreground="#86EFAC" FontSize="9" FontWeight="Bold"/>
       </Border>
       <TextBlock Text="Ví và nạp tiền" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

// Tab 5: Hồ sơ
xaml = xaml.replace('<TabItem Header="Hồ sơ">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="AccountCircleOutline" Width="18" Height="18" Foreground="#60A5FA" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <TextBlock Text="Hồ sơ" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

// Tab 6: Hỗ trợ
xaml = xaml.replace('<TabItem Header="Hỗ trợ">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="Headset" Width="18" Height="18" Foreground="#F472B6" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <TextBlock Text="Hỗ trợ" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

// Tab 7: Khảo sát
xaml = xaml.replace('<TabItem Header="Khảo sát">', `<TabItem>
     <TabItem.Header>
      <DockPanel Width="146">
       <md:PackIcon Kind="ClipboardCheckOutline" Width="18" Height="18" Foreground="#2DD4BF" VerticalAlignment="Center" Margin="0,0,10,0"/>
       <TextBlock Text="Khảo sát" FontSize="13" VerticalAlignment="Center"/>
      </DockPanel>
     </TabItem.Header>`);

fs.writeFileSync('InternetCafe/apps/desktop-client/InternetCafe.Client/MainWindow.xaml', xaml, 'utf8');
console.log('Sidebar upgraded successfully in MainWindow.xaml!');
