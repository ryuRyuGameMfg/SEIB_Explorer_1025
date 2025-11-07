!*** NOTE ***
!In case of the Intel Fortran compiler, use the option '-assume byterecl' as fillows.
!　　　ifort start_point.f90 -o go.out -assume byterecl

!*************************************************************************************************
! Start up procedures for selected sites
! (read common parameters and climate data)
!*************************************************************************************************
   Include 'modules.f90'        
   Include 'main_point.f90'     
   Include 'initialize.f90'     
   Include 'metabolic.f90'      
   Include 'output.f90'         
   Include 'physics.f90'        
   Include 'population_regu.f90'
   Include 'spatial_calc.f90'   
   Include 'etc.f90'            
   Include 'SFLXALL_SRC_VER_2.7.1.f90'
   
PROGRAM start_point
   USE data_structure
   USE iso_fortran_env !★ [For SEIB-Explorer]
   implicit none
   
!_____________★ [For SEIB-Explorer]

!SEIB-Explorerと連携して使用する場合には★ [For SEIB-Explorer]部分をActivateすること
   
   !*はmain_point.f90に受け渡す変数
   
   !Unityとの通信に用いるファイル・フォルダ
   !なお、トップフォルダは、SEIB-Explorerインストールフォルダ直下の/External
   character(len=256),parameter:: FileName_Param = "input/params.csv" !入力パラメーターファイル
   character(len=256),parameter:: FileName_Stop  = "control/stop.txt" !停止指令ファイル *
   character(len=256),parameter:: FileName_Abort = "control/abort.txt" !停止信号ファイル *
   character(len=256),parameter:: PathName_Out   = "output/"          !出力ファイル書き出し先 * 
   
   !Unity出力のパラメーターファイルから読み出す変数
   real               :: Input_ModTmp	    !変化量ユーザー入力：気温
   real               :: Input_ModTmpRange	!変化量ユーザー入力：日気温変化量
   real               :: Input_ModRH		!変化量ユーザー入力：相対湿度
   real               :: Input_ModPrecip  	!変化量ユーザー入力：降水量
   real               :: Input_ModRad		!変化量ユーザー入力：下向き短波放射強度
   real               :: Input_ModCO2     	!変化量ユーザー入力：CO2濃度
   character(len=100) :: Input_SiteName   !シミュレーションサイト名称
   integer            :: Input_SimYear    !シミュレーション年数
   character(len=100) :: Input_FN_Result  !ファイル名、結果の出力ファイル名（拡張子無し）*
   
   character(len=100) :: Input_FN_Climate !ファイル名、CO2入力（フルパス＋ファイル名）
   character(len=100) :: Input_FN_CO2     !ファイル名、機構入力（拡張子無し）
   real               :: Input_latitude   !緯度
   real               :: Input_longitude  !経度
   
   !IO制御に用いる変数
   character(len=128) :: line
character(len=512) :: arg, io_root
integer :: argc
   
!_____________ Set Variables
!Set year number of climate CO2 data
   integer YearMaxClimate !year length of the climate data
   integer YearMaxCO2     !year length of the CO2 data
   
!Climate data
   real,allocatable,dimension(:,:)  ::&
   tmp_air       ,& ! 1. Surface air temperature (Celcius)
   tmp_air_range ,& ! 2. Daily range of tmp_air (Celcius)
   prec          ,& ! 3. Precipitation (mm day-1)
   rad_short     ,& ! 4. Shortwave radiation, downward, @ midday (W m-2)
   rad_long      ,& ! 5. Daily mean of longwave radiation, downward (W m-2)
   wind          ,& ! 6. Wind velocity (m s-1)
   rh            ,& ! 7. Relative humidity (%)
   tmp_soil1     ,& ! 8. Soil temperature   0- 10cm depth (Celcius)
   tmp_soil2     ,& ! 9. Soil temperature  10-200cm depth (Celcius)
   tmp_soil3        !10. Soil temperature 200-300cm depth (Celcius)
   
   real,allocatable,dimension(:,:,:)::tmp_soil   !Soil temperature for each layers (Celcius)
   
!Atomospheric CO2 time-series @ ppm
   real,allocatable,dimension(:)::aco2_annual
   
!Location data
   integer Mask         !Land ocean mask (1:land, 0:ocean)
   real    ALT          !altitude (m above MSL)
   real    Albedo_soil0 !albedo, default
   real    W_fi         !filed capacity   (m3/m3, 0.0 -> 1.0)
   real    W_wilt       !wilting point    (m3/m3, 0.0 -> 1.0)
   real    W_sat        !saturate point   (m3/m3, 0.0 -> 1.0)
   real    W_mat        !matrix potential (m, -0.0001 -> -3.0)
   
   integer SoilClass    !Zobler(1986)'s slope type index 1-9 (default = 1)
   ! SOIL TYPES   ZOBLER (1986)	  COSBY ET AL (1984) (quartz cont.(1))
   !  1	    COARSE	      LOAMY SAND	 (0.82)
   !  2	    MEDIUM	      SILTY CLAY LOAM	 (0.10)
   !  3	    FINE	      LIGHT CLAY	 (0.25)
   !  4	    COARSE-MEDIUM     SANDY LOAM	 (0.60)
   !  5	    COARSE-FINE       SANDY CLAY	 (0.52)
   !  6	    MEDIUM-FINE       CLAY LOAM 	 (0.35)
   !  7	    COARSE-MED-FINE   SANDY CLAY LOAM	 (0.60)
   !  8	    ORGANIC	      LOAM		 (0.40)
   !  9	    GLACIAL LAND ICE  LOAMY SAND	 (NA using 0.82)
   !  0	    Ocean / Water body
   
!Others
   integer GlobalZone     !ID number of global zone
   real    CTI_dif        !Difference in CTI (Composite Terrestrial Index)
                          !CTI of grid average - CTI of this plot
   
   integer point          !Loop counter
   integer count          !Loop counter
   integer count_alt      !Loop counter
   integer count_cti      !Loop counter
   integer i, j           !for general usage
   real    x, y           !for general usage
   character(len=256):: chr_tmp !for counting number of data item
   integer ios               !IO error statusl
   character(len=200) :: msg !IO error process
   
!_____________ Read Parameters
!Read Parameter files
   open (1, file='parameter.txt', action='READ', status='OLD', iostat=ios, iomsg=msg)
   if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Control     , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=PFT_type    , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Respiration , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Turnover_n  , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Metabolic   , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Assimilation, iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Dynamics    , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Disturbance , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   read (unit=1, nml=Soil_resp   , iostat=ios,iomsg=msg);if (ios /= 0) call error_handler(ios,msg)
   close (1)
   !Memo of location (LAT,LON)
   ! 44.82 , 142.08 : Nakagawa, Hokkaido, Japan
   ! 42.95 , 141.2  : Jyozankei, Hokkaido, Japan
   ! 42.623, 141.546: TEF plot2, Hokkaido, Japan
   ! 42.772, 141.407: Shikotsuko (Tomakomai site), Hokkaido, Japan
   ! 62.017, 129.717: Sppaskaya-pad
   !  2.97 , 102.3  : Pasoh
   
!_____________ ★[For SEIB-Explorer] 制御ファイルを読んで、それに基づいて各種設定を変更
  argc = command_argument_count()
  io_root = ''  ! default: 空なら内部で決める
  
  i = 1
  do while (i <= argc)
     call get_command_argument(i, arg)
     if (trim(arg) == '--io-root' .and. i < argc) then
        call get_command_argument(i+1, io_root)
        i = i + 2
     else
        if (len_trim(io_root) == 0) io_root = trim(arg)  ! 後方互換：旧仕様（パスのみ）も許可
        i = i + 1
     end if
  end do
  
  if (len_trim(io_root) == 0) then
     ! 互換用：未指定ならカレントや既定値を使用
     io_root = './'
  end if

   Open(unit=10, file=trim(io_root)//FileName_Param, status='old', action='read', iostat=ios)
      if (ios /= 0) then
        print *, "[Fortran] 入力ファイルのオープンに失敗: ", trim(trim(io_root)//FileName_Param)
        stop
      end if
      
      ! 1行目（ヘッダー）を読み飛ばす
      read(10, '(A)', iostat=ios) line
      if (ios /= 0) then
        print *, "[Fortran] ヘッダー読み飛ばしに失敗"
        stop
      end if
      
      ! 2行目（値）を読む
      read(10, *, iostat=ios) &
          Input_ModTmp,      &
          Input_ModTmpRange, &
          Input_ModRH,       &
          Input_ModPrecip,   &
          Input_ModRad,      &
          Input_ModCO2,      &
          Input_SiteName,    &
          Input_FN_Result,   &
          Input_SimYear,     &
          Input_latitude,    &
          Input_longitude,   &
          Input_FN_CO2,      &
          Input_FN_Climate    
      
   Close(10)
      
      if (ios /= 0) then
        print *, "[Fortran] 値読み込み失敗"
        stop
      end if
      print *, "[Fortran] パラメータ読み込み完了"
   
   !"Input_SiteName"に基づいて、緯度・経度・入力気候データファイル名を差し替える
   if     (trim(Input_SiteName) == "Nakagawa") then
      LAT        =  44.82
      LON        = 142.08
      Fn_climate = "climate_Nakagawa_1981_1990.txt"
      Est_scenario    = 1
      Est_pft_OnOff   = (/ &
                        .false.,.false.,.false.,.false.,.false.,.false., &
                        .false.,.false.,.false.,                         &
                        .true., .true., .true., .true., .true., .true.,  &
                        .false.,.false.,.false.,.false.,.false.,         &
                        .false.,.false.                                 /)
      
   elseif (trim(Input_SiteName) == "SppaskayaPad") then
      LAT        =  62.017
      LON        = 129.717
      Fn_climate = "climate_Sppaskaya_1981_1990.txt"
      Est_scenario    = 1
      Est_pft_OnOff   = (/ &
                        .false.,.false.,.false.,.false.,.false.,.false., &
                        .false.,.false.,.false.,                         &
                        .false.,.false.,.false.,.false.,.false.,.false., &
                        .false.,.false.,.false., .true., .true.,         &
                        .false.,.false.                                 /)
     
   elseif (trim(Input_SiteName) == "Pasoh") then
      LAT        =   2.97
      LON        = 102.3
      Fn_climate = "climate_AutoGenerated_Pasoh.txt"
      Est_scenario    = 1
      Est_pft_OnOff   = (/ &
                        .true., .true., .true., .true., .true., .true., &
                        .false.,.false.,.false.,                         &
                        .false.,.false.,.false.,.false.,.false.,.false.,       &
                        .false.,.false.,.false.,.false.,.false.,         &
                        .false.,.false.                                 /)
      
   elseif (trim(Input_SiteName) == "I_Specify_It") then
      LAT        = Input_latitude
      LON        = Input_longitude
      Fn_climate = Input_FN_Climate
      Fn_CO2     = Input_FN_CO2
   endif
   
   !シミュレーション年数
   Simulation_year = Input_SimYear
   
   
   !不要なファイルは書き出さないようにする
   Flag_output_write=.false.
   Flag_spinup_write=.false.
   
!_____________ 読み込んだ緯度と経度を元に、GlobalZoneや土壌データの準備
!Set cordinate variables
   point = (90-int(LAT)-1)*360 + int(LON+180) + 1
   
!GlobalZone: Set Location category
   if (LON>=-20 .and. 60>=LON .and. 23.0>=LAT ) then
      !African continent
      GlobalZone = 1
   elseif (LON>=100 .and. 170>=LON .and. 50.0<=LAT) then
      !Eastern Siberia
      GlobalZone = 2
   else
      !Default
      GlobalZone = 0
   endif
   
!CTI_dif: CTI adjuster
   CTI_dif = 0.0
   
!Read Location data
   open ( File_no(1), file=Fn_location, status='OLD', iostat=ios, iomsg=msg)
   if (ios /= 0) call error_handler(ios, msg)
   do i=1, point
      read(File_no(1),*) Mask, ALT, Albedo_soil0, W_sat, W_fi, W_mat, W_wilt, SoilClass
   end do
   close( File_no(1) )
   
   !data processing
   if (W_fi   > W_sat ) W_fi   = W_sat
   if (W_wilt > W_sat ) W_wilt = W_sat
   
![Option] Soil properties by Hazeltine & Prentice (1996), which is also employed by Sato et al (2010)
!   W_sat  = 0.443
!   W_fi   = 0.399
!   W_wilt = 0.211
   
![Option] Soil properties of SOILTYP=6 of NOAH-LSM
!   W_sat  = 0.465
!   W_fi   = 0.465
!   W_wilt = 0.103
   
   !★ [For SEIB-Explorer] 
   if (Mask==0) then
      print *, "[Fortran] The specified coordinates are determined to be non-terrestrial."
      print *, "[Fortran] Process aborted"
      
      open(unit=10, file=trim(io_root)//FileName_Abort, status='new')
      close(10)
      
      stop
   end if

!_____________ Prepare Climate Data
!Count line numbers in climate-data-file
   count = 0
   open (File_no(1), file=Fn_climate, status='OLD', iostat=ios, iomsg=msg)
   if (ios /= 0) call error_handler(ios, msg)
   do i=1,Day_in_Year * 10000
      count = count + 1
      read(File_no(1),'(a)',end=100) chr_tmp
   enddo
   100 continue
   close (File_no(1))
   YearMaxClimate = int(count/Day_in_Year)
   
!Count item number in each line of the climate-data-file
   chr_tmp = trim(chr_tmp)
   do count=1, 10
      j = SCAN (chr_tmp,',')
      if (j==0) exit
      chr_tmp = chr_tmp((j+1):)
   enddo
   
!Set sizes of allocatable climate data table
   allocate (tmp_air       (Day_in_Year, YearMaxClimate)     )
   allocate (tmp_air_range (Day_in_Year, YearMaxClimate)     )
   allocate (prec          (Day_in_Year, YearMaxClimate)     )
   allocate (rad_short     (Day_in_Year, YearMaxClimate)     )
   allocate (rad_long      (Day_in_Year, YearMaxClimate)     )
   allocate (wind          (Day_in_Year, YearMaxClimate)     )
   allocate (rh            (Day_in_Year, YearMaxClimate)     )
   allocate (tmp_soil1     (Day_in_Year, YearMaxClimate)     )
   allocate (tmp_soil2     (Day_in_Year, YearMaxClimate)     )
   allocate (tmp_soil3     (Day_in_Year, YearMaxClimate)     )
   
   allocate (tmp_soil      (Day_in_Year, YearMaxClimate, NumSoil) )
   
!Read Climatic data
   open (File_no(1), file=Fn_climate, status='OLD', iostat=ios, iomsg=msg)
   if (ios /= 0) call error_handler(ios, msg)
   do i=1, YearMaxClimate
   do j=1, Day_in_Year
      if (count==7) then
         !In case soil temperatures data is NOT available
         read (File_no(1),*) tmp_air(j,i), tmp_air_range(j,i), &
                             prec(j,i), rad_short(j,i), rad_long(j,i), wind(j,i), rh(j,i)
         tmp_soil1(j,i) = tmp_air(j,i)
         tmp_soil2(j,i) = tmp_air(j,i)
         tmp_soil3(j,i) = tmp_air(j,i)
      else
         !In case soil temperatures data is available
         read (File_no(1),*) tmp_air(j,i), tmp_air_range(j,i), &
                             prec(j,i), rad_short(j,i), rad_long(j,i), wind(j,i), rh(j,i), &
                             tmp_soil1(j,i), tmp_soil2(j,i), tmp_soil3(j,i)
      endif
      
      !Give adhock values for soil temperature
      Call tmp_soil_interpolate (tmp_soil1(j,i), tmp_soil2(j,i), tmp_soil3(j,i), tmp_soil(j,i,:))
   enddo
   enddo
   close (File_no(1))
   
   !______________ Read time-series of atmospheric CO2
!Scan data length of CO2 time series
   YearMaxCO2 = 0
   open (File_no(1), file=Fn_CO2, status='OLD', iostat=ios, iomsg=msg)
   if (ios /= 0) call error_handler(ios, msg)
   do i=1, 10000
      YearMaxCO2 = YearMaxCO2 + 1
      read(File_no(1),*,end=200)
   enddo
   200 continue
   close (File_no(1))
   YearMaxCO2 = YearMaxCO2 - 1
   
!Set sizes of allocatable CO2 data table
   allocate ( aco2_annual(YearMaxCO2) )
   
!Read CO2 data
   Open (File_no(1), file=Fn_CO2, status='OLD', iomsg=msg)
   if (ios /= 0) call error_handler(ios, msg)
   do i = 1, YearMaxCO2
      read(File_no(1), *) aco2_annual(i)
   end do
   Close (File_no(1))
   
   !★______________ [For SEIB-Explorer] Unity側からの指定に基づいて気候データを改変する
   do i=1, YearMaxClimate
   do j=1, Day_in_Year
      tmp_air      (j,i) = tmp_air       (j,i) + Input_ModTmp
      tmp_soil1    (j,i) = tmp_soil1     (j,i) + Input_ModTmp
      tmp_soil2    (j,i) = tmp_soil2     (j,i) + Input_ModTmp
      tmp_soil3    (j,i) = tmp_soil3     (j,i) + Input_ModTmp
      tmp_air_range(j,i) = tmp_air_range (j,i) * Input_ModTmpRange
      rh           (j,i) = rh            (j,i) * Input_ModRH
      prec         (j,i) = prec          (j,i) * Input_ModPrecip
      rad_short    (j,i) = rad_short     (j,i) * Input_ModRad
      
      !存在し得ない値を避けるための安全弁
      tmp_air_range(j,i) = max(0.0,            tmp_air_range (j,i))
      rh           (j,i) = max(0.0, min(100.0, rh            (j,i)))
      prec         (j,i) = max(0.0,            prec          (j,i))
      rad_short    (j,i) = max(0.0,            rad_short     (j,i))
   enddo
   enddo
   
   do i = 1, YearMaxCO2
      aco2_annual(i) = max(0.0, aco2_annual(i) + Input_ModCO2)
   end do
   
   
!___________ For employing different random seed for each run (by Shigeki Ikeda @ Kyoto Univ.)
   IF (Flag_randomization) then
      call random_seed(size=seedsize)
      allocate(seed(seedsize))
      
      do size_count=1,seedsize
      call system_clock(count=clock)
      seed(size_count)=clock
      end do
      
      call random_seed(put=seed)
   EndIf
   
	!_____________ Display Simulation Conditions
   !Print location properties
   write (*,*)
   write (*,*) '*********  Coodinate Configurations  *********'
   write (*,*) 'Latitude, Longtitude   :', LAT, LON
   write (*,*) 'Point nomal            :', point
   write (*,*)
   
   !Print soil properties
   write (*,*) '*********  Location properties  *********'
   write (*,*) 'Altitude    :', ALT         
   write (*,*) 'Albedo_soil0:', Albedo_soil0
   write (*,*) 'W_sat       :', W_sat       
   write (*,*) 'W_fi        :', W_fi        
   write (*,*) 'W_wilt      :', W_wilt      
   write (*,*)                              
   
   !Print climate properties
   x = real(Day_in_Year * YearMaxClimate)
   y = real(YearMaxClimate)
   
   write (*,*) '*********  Wether statistics (annual mean of the all years)  *********'
   write (*,*) 'Air temperature         (Cecius)  :', sum(tmp_air       (:,:)) / x
   write (*,*) 'Daily range of Air tmp. (Cecius)  :', sum(tmp_air_range (:,:)) / x
   write (*,*) 'Ralative Humidity       (%)       :', sum(rh            (:,:)) / x
   write (*,*) 'Precipitation           (mm/year) :', sum(prec          (:,:)) / y
   write (*,*) 'Shortwave Rad. @ midday (W m-2)   :', sum(rad_short     (:,:)) / x
   write (*,*) 'wind                    (m/s)     :', sum(wind     (:,:)) / x
   write (*,*) 
   
!_____________ Simulation
   !Call simulation loop
   IF (Mask==0 .or. Albedo_soil0<=0.0 .or. W_sat<=0.0 .or. W_fi<=0.0 .or. W_wilt<=0.0 ) then
      write(*,*) 'Error: Invalid location properties'
   ELSE
      write (*,*) '*********  Now simulating  *********'

![OPTION] For Hokkaido Japan (See Sato et al., 2023 Ecol. Res. for detail)
 CTI_dif = 0.0 !Grid average            （CTI= 4.7          ）
!CTI_dif = 1.1 !For relatively dry plots（CTI= 4.7-1.1 = 3.6）
!CTI_dif =-2.5 !For relatively wet plots (CTI= 4.7+2.5 = 7.2）

   Call main_loop ( &
   GlobalZone, YearMaxClimate, YearMaxCO2, &
   tmp_air(:,:), tmp_air_range(:,:), prec(:,:), rad_short(:,:), rad_long(:,:), &
   wind(:,:), rh(:,:), tmp_soil(:,:,:), &
   aco2_annual(:), ALT, Albedo_soil0, W_fi, W_wilt, W_sat, W_mat, SoilClass, CTI_dif, &
   !★ [For SEIB-Explorer] 
   trim(io_root)//FileName_Stop, trim(io_root)//PathName_Out, Input_FN_Result)
   
   write (*,*) '*********  Done  *********'
   END IF
   
   STOP
   
END PROGRAM start_point

!Error suring IO process
SUBROUTINE error_handler(ios, msg)
   USE data_structure
   implicit none
   
   integer           ,intent(IN):: ios !status of file open
   character(len=200),intent(IN):: msg !IO error process
   
   !★ [For SEIB-Explorer] 
   open  (File_no(1), file = 'control/IO_error.txt', status="new")
      write(File_no(1),  '(i2)') ios
      write(File_no(1), '(a100)') trim(msg)
   close (File_no(1))
   
   print *, "[Fortran] An error occurred while reading the file. Aborting."
   print *, "ios flag:", ios, trim(msg)
   
   STOP

END SUBROUTINE error_handler
